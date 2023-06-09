#pragma once
#include "tbprobe.h"

using namespace System;

namespace Pedantic 
{
    namespace Tablebase
    {
        public enum class TbGameResult : char
        {
            Loss = 0, BlessedLoss, Draw, CursedWin, Win
        };

        public enum class TbPromotes : char
        {
            None = 0, Queen, Rook, Bishop, Knight
        };

        public value struct TbResult
        {
        public:
            unsigned int result;

        public:
            static TbResult()
            {
                TbWin.result = TB_SET_WDL(0, static_cast<unsigned int>(TbGameResult::Win));
                TbDraw.result = TB_SET_WDL(0, static_cast<unsigned int>(TbGameResult::Draw));
                TbLoss.result = TB_SET_WDL(0, static_cast<unsigned int>(TbGameResult::Loss));
                TbFailure.result = TB_RESULT_FAILED;
            }

            static bool operator == (TbResult res1, TbResult res2)
            {
                return res1.result == res2.result;
            }
            static bool operator != (TbResult res1, TbResult res2)
            {
                return res1.result != res2.result;
            }
            property TbGameResult Wdl
            {
                TbGameResult get()
                {
                    return static_cast<TbGameResult>(TB_GET_WDL(result));
                }
                void set(TbGameResult wdl)
                {
                    unsigned int nWdl = static_cast<unsigned int>(wdl);
                    TB_SET_WDL(result, nWdl);
                }
            }
            property unsigned int From
            {
                unsigned int get()
                {
                    return TB_GET_FROM(result);
                }
                void set(unsigned int from)
                {
                    TB_SET_FROM(result, from);
                }
            }
            property unsigned int To
            {
                unsigned int get()
                {
                    return TB_GET_TO(result);
                }
                void set(unsigned int to)
                {
                    TB_SET_TO(result, to);
                }
            }
            property unsigned int Promotes
            {
                unsigned int get()
                {
                    return TB_GET_PROMOTES(result);
                }
                void set(unsigned int promotes)
                {
                    TB_SET_PROMOTES(result, promotes);
                }
            }
            property bool Ep
            {
                bool get()
                {
                    return TB_GET_EP(result) != 0;
                }
                void set(bool ep)
                {
                    TB_SET_EP(result, ep ? 1 : 0);
                }
            }
            property unsigned int Dtz
            {
                unsigned int get()
                {
                    return TB_GET_DTZ(result);
                }
                void set(unsigned int dtz)
                {
                    TB_SET_DTZ(result, dtz);
                }
            }
            initonly static TbResult TbWin;
            initonly static TbResult TbDraw;
            initonly static TbResult TbLoss;
            initonly static TbResult TbFailure;
        };

        public value struct TbMove
        {
        public:
            unsigned short move;

            property unsigned short From
            {
                unsigned short get()
                {
                    return TB_MOVE_FROM(move);
                }
            }

            property unsigned short To
            {
                unsigned short get()
                {
                    return TB_MOVE_TO(move);
                }
            }

            property unsigned short Promotes
            {
                unsigned short get()
                {
                    return TB_MOVE_PROMOTES(move);
                }
            }
        };

        public value struct TbRootMove
        {
        public:
            TbMove move;
            array<TbMove>^ pv;
            int tbScore, tbRank;
            
            TbRootMove(::TbRootMove rm)
            {
                move.move = rm.move;
                pv = gcnew array<TbMove>(rm.pvSize);
                for (unsigned int n = 0; n < rm.pvSize; ++n)
                {
                    pv[n].move = rm.pv[n];
                }
                tbScore = rm.tbScore;
                tbRank = rm.tbRank;
            }
        };

	    public ref class Syzygy abstract sealed
	    {
        public:

            /// <summary>
            /// Initialize the tablebase.
            /// </summary>
            /// <param name="path">The tablebase PATH string.</param>
            /// <returns>
            /// - true=success, false=failed. The <c>TbLargest</c> property will also
            /// be initialized. If no tablebase files are found, then true is returned
            /// and <c>TbLargest</c> is set to zero.
            /// </returns>
            static bool Initialize(String^ path)
            {
                IntPtr p = System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(path);
                char *pPath = static_cast<char*>(p.ToPointer());
                bool result = ::tb_init(pPath);
                System::Runtime::InteropServices::Marshal::FreeHGlobal(p);            
                return result;
            }
            
            /// <summary>
            /// Free any resources allocated by tb_init().
            /// </summary>
            static void Uninitialize()
            {
                ::tb_free();
            }
            
            /// <summary>
            /// Probe the Win-Draw-Loss (WDL) table.
            /// </summary>
            /// <param name="white">The white piece bitboard</param>
            /// <param name="black">The black piece bitboard</param>
            /// <param name="kings">The kings bitboard</param>
            /// <param name="queens">The queens bitboard</param>
            /// <param name="rooks">The rooks bitboard</param>
            /// <param name="bishops">The bishops bitboard</param>
            /// <param name="knights">The knights bitboard</param>
            /// <param name="pawns">The pawns bitboard</param>
            /// <param name="rule50">The 50-move half-move clock.</param>
            /// <param name="castling">The castling rights. Set to zero if no castling possible.</param>
            /// <param name="ep">
            ///     The en passant square (if exists). Set to zero if there is no en passant square.
            /// </param>
            /// <param name="wtm">
            ///     White's turn to move flags. Set to true if it is the white pieces turn to move.
            /// </param>
            /// <returns>
            /// Pedantic.Tablebase.TbResult - One of Wdl == { Loss, BlessedLoss, Draw, CursedWin, Win }, or
            /// TbResult.Failure if the probe failed.
            /// </returns>
            /// <remarks>
            ///     Engines should use this method during search. This method is thread-safe.
            /// </remarks>
            static TbResult ProbeWdl(
                unsigned long long white,
                unsigned long long black,
                unsigned long long kings,
                unsigned long long queens,
                unsigned long long rooks,
                unsigned long long bishops,
                unsigned long long knights,
                unsigned long long pawns,
                unsigned int rule50,
                unsigned int castling,
                unsigned int ep,
                bool wtm
            )
            {
                TbResult tbResult;
                
                tbResult.result = ::tb_probe_wdl(
                    white, black, kings, queens, rooks, bishops, knights, pawns, rule50, castling, ep, wtm
                );
                return tbResult;
            }
            
            /// <summary>
            /// Probes the Distance-To-Zero (DTZ) table.
            /// </summary>
            /// <param name="white">The white piece bitboard</param>
            /// <param name="black">The black piece bitboard</param>
            /// <param name="kings">The kings bitboard</param>
            /// <param name="queens">The queens bitboard</param>
            /// <param name="rooks">The rooks bitboard</param>
            /// <param name="bishops">The bishops bitboard</param>
            /// <param name="knights">The knights bitboard</param>
            /// <param name="pawns">The pawns bitboard</param>
            /// <param name="rule50">The 50-move half-move clock.</param>
            /// <param name="castling">The castling rights. Set to zero if no castling possible.</param>
            /// <param name="ep">
            ///     The en passant square (if exists). Set to zero if there is no en passant square.
            /// </param>
            /// <param name="wtm">
            ///     White's turn to move flags. Set to true if it is the white pieces turn to move.
            /// </param>
            /// <param name="results">
            ///     The results (OPTIONAL) - Alternative results, one for each
            ///     possible legal moves. If alternative results are not desired then set results = null.
            /// </param>
            /// <returns>
            ///     A TbResult value comprising:
            ///     <ol>
            ///         <li>The WDL value</li>
            ///         <li>The suggested move</li>
            ///         <li>The DTZ value
            ///     </ol>
            ///     The suggested move is guaranteed to preserve the WDL value.
            ///     
            ///     Otherwise:
            ///     <ol>
            ///         <li>TbResult.Stalemate is returned if the position is in stalemate.</li>
            ///         <li>TbResult.Checkmate is returned if the position is in checkmate.</li>
            ///         <li>TbResult.Failure is returned if the probe failed.
            ///     </ol>
            /// 
            ///     If results != null, then a TbResult for each legal move will be generated and
            ///     stored in the results array.
            /// </returns>
            /// <remarks>
            ///     NOTES:
            ///     <ul>
            ///         <li>
            ///             Engines can use this method to probe at the root. This method should not 
            ///             be used during search.
            ///         </li>
            ///         <li>
            ///             DTZ tablebases can suggest unnatural moves, especially for losing positions.
            ///             Engines may prefer to traditional search combined with WDL move filtering
            ///             using the alternative results array.
            ///         </li>
            ///         <li>
            ///             This method is NOT thread-safe. For engines this method should only be 
            ///             called once at the root per search.
            ///         </li>
            ///     </ul>
            /// </remarks>
            static TbResult ProbeRoot(
                unsigned long long white,
                unsigned long long black,
                unsigned long long kings,
                unsigned long long queens,
                unsigned long long rooks,
                unsigned long long bishops,
                unsigned long long knights,
                unsigned long long pawns,
                unsigned int rule50,
                unsigned int castling,
                unsigned int ep,
                bool wtm,
                array<TbResult>^ results
            )
            {
                unsigned int res[TB_MAX_MOVES];
                TbResult tbResult;

                if (results == nullptr)
                {
                    tbResult.result = ::tb_probe_root(
                        white, black, kings, queens, rooks, bishops, knights, pawns, rule50, castling, ep, wtm, __nullptr
                    );
                }
                else
                {
                    tbResult.result = ::tb_probe_root(
                        white, black, kings, queens, rooks, bishops, knights, pawns, rule50, castling, ep, wtm, res
                    );
                    if (tbResult != TbResult::TbFailure)
                    {
                        unsigned int arraySize = 0;
                        for (; arraySize < TB_MAX_MOVES && res[arraySize] != TB_RESULT_FAILED; ++arraySize)
                            ;

                        array<TbResult>::Resize(results, arraySize);
                        for (unsigned int n = 0; n < arraySize; ++n)
                        {
                            TbResult tbRes;
                            tbRes.result = res[n];
                            results[n] = tbRes;
                        }
                    }
                }
                return tbResult;
            }
            
            /// <summary>
            /// Use the DTZ tables to rank and score all root moves.
            /// </summary>
            /// <param name="white">The white piece bitboard</param>
            /// <param name="black">The black piece bitboard</param>
            /// <param name="kings">The kings bitboard</param>
            /// <param name="queens">The queens bitboard</param>
            /// <param name="rooks">The rooks bitboard</param>
            /// <param name="bishops">The bishops bitboard</param>
            /// <param name="knights">The knights bitboard</param>
            /// <param name="pawns">The pawns bitboard</param>
            /// <param name="rule50">The 50-move half-move clock.</param>
            /// <param name="castling">The castling rights. Set to zero if no castling possible.</param>
            /// <param name="ep">
            ///     The en passant square (if exists). Set to zero if there is no en passant square.
            /// </param>
            /// <param name="wtm">
            ///     White's turn to move flags. Set to true if it is the white pieces turn to move.
            /// </param>
            /// <param name="hasRepeated">
            ///     If true indicates that the current position has already been repeated in the 
            ///     reversible lookback period.
            /// </param>
            /// <param name="useRule50">
            ///     Helps to determine the border between winning and drawn positions.
            /// </param>
            /// <param name="rootMoves">
            ///     If probe is success, this array will contain all of the legal root moves, their rank,
            ///     score, and a predicted PV.
            /// </param>
            /// <returns>
            ///     non-zero if ok, 0 means not all probes were successful
            /// </returns>
            static int ProbeRootDtz(
                unsigned long long white,
                unsigned long long black,
                unsigned long long kings,
                unsigned long long queens,
                unsigned long long rooks,
                unsigned long long bishops,
                unsigned long long knights,
                unsigned long long pawns,
                unsigned int rule50,
                unsigned int castling,
                unsigned int ep,
                bool wtm,
                bool hasRepeated,
                bool useRule50,
                array<TbRootMove>^% rootMoves
            )
            {
                ::TbRootMoves* pRootMoves = new ::TbRootMoves;
                int result = ::tb_probe_root_dtz(
                    white, black, kings, queens, rooks, bishops, knights, pawns, rule50, castling, ep, wtm, 
                    hasRepeated, useRule50, pRootMoves
                );

                if (result != 0)
                {
                    rootMoves = gcnew array<TbRootMove>(pRootMoves->size);
                    for (unsigned int n = 0; n < pRootMoves->size; ++n)
                    {
                        TbRootMove rootMove(pRootMoves->moves[n]);
                        rootMoves[n] = rootMove;
                    }
                }
                else
                {
                    rootMoves = gcnew array<TbRootMove>(0);
                }
                delete pRootMoves;
                return result;
            }
            
            /// <summary>
            /// Use the WDL tables to rank and score all root moves. This is a fallback for the 
            /// case that some or all DTZ tables are missing.
            /// </summary>
            /// <param name="white">The white piece bitboard</param>
            /// <param name="black">The black piece bitboard</param>
            /// <param name="kings">The kings bitboard</param>
            /// <param name="queens">The queens bitboard</param>
            /// <param name="rooks">The rooks bitboard</param>
            /// <param name="bishops">The bishops bitboard</param>
            /// <param name="knights">The knights bitboard</param>
            /// <param name="pawns">The pawns bitboard</param>
            /// <param name="rule50">The 50-move half-move clock.</param>
            /// <param name="castling">The castling rights. Set to zero if no castling possible.</param>
            /// <param name="ep">
            ///     The en passant square (if exists). Set to zero if there is no en passant square.
            /// </param>
            /// <param name="wtm">
            ///     White's turn to move flags. Set to true if it is the white pieces turn to move.
            /// </param>
            /// <param name="useRule50">
            ///     Helps to determine the border between winning and drawn positions.
            /// </param>
            /// <param name="rootMoves">
            ///     If probe is success, this array will contain all of the legal root moves, their rank,
            ///     score, and a predicted PV.
            /// </param>
            /// <returns>
            ///     non-zero if ok, 0 means not all probes were successful
            /// </returns>
            static int ProbeRootWdl(
                unsigned long long white,
                unsigned long long black,
                unsigned long long kings,
                unsigned long long queens,
                unsigned long long rooks,
                unsigned long long bishops,
                unsigned long long knights,
                unsigned long long pawns,
                unsigned int rule50,
                unsigned int castling,
                unsigned int ep,
                bool wtm,
                bool useRule50,
                array<TbRootMove>^% rootMoves
            )
            {
                ::TbRootMoves* pRootMoves = new ::TbRootMoves;
                int result = ::tb_probe_root_wdl(
                    white, black, kings, queens, rooks, bishops, knights, pawns, rule50, castling, ep, wtm, 
                    useRule50, pRootMoves
                );

                if (result != 0)
                {
                    rootMoves = gcnew array<TbRootMove>(pRootMoves->size);
                    for (unsigned int n = 0; n < pRootMoves->size; ++n)
                    {
                        TbRootMove rootMove(pRootMoves->moves[n]);
                        rootMoves[n] = rootMove;
                    }
                }
                else
                {
                    rootMoves = gcnew array<TbRootMove>(0);
                }
                delete pRootMoves;
                return result;
            }
            
            /// <summary>
            /// The tablebase can be probed for any position where #pieces <= TbLargest.
            /// </summary>
            static property unsigned int TbLargest
            {
                unsigned int get()
                {
                    return TB_LARGEST;
                }
            }
	    };
    }
}
