using System.Net.NetworkInformation;
using Pedantic.Utilities;

namespace Pedantic.Chess
{
    public sealed partial class Board
    {
        #region Magic Bitboards for Sliding Move Generation

        public static ulong GetBishopAttacksMagic(int square, ulong blockers)
        {
            blockers &= bishopMasks[square];
            int index = square << 10;
            index += (int)((blockers * bishopMagics[square]) >> (64 - bishopIndexBits[square]));
            return bishopTable[index];
        }

        public static ulong GetRookAttacksMagic(int square, ulong blockers)
        {
            blockers &= rookMasks[square];
            int index = square << 12;
            index += (int)((blockers * rookMagics[square]) >> (64 - rookIndexBits[square]));
            return rookTable[index];
        }

        private static ulong GetBlockersFromIndex(int index, ulong mask)
        {
            ulong blockers = 0ul;
            int bits = BitOps.PopCount(mask);
            for (int i = 0; i < bits; ++i)
            {
                int bitPos = BitOps.TzCount(mask);
                mask = BitOps.ResetLsb(mask);
                if ((index & (1 << i)) != 0)
                {
                    blockers |= 1ul << bitPos;
                }
            }

            return blockers;
        }

        private static void InitPieceMasks()
        {
            ulong edgeSquares = maskRanks[0] | maskRanks[63] | maskFiles[0] | maskFiles[7];

            for (int sq = 0; sq < Constants.MAX_SQUARES; ++sq)
            {
                Ray ray = vectors[sq];
                bishopMasks[sq] = ray.NorthEast | ray.NorthWest | ray.SouthEast | ray.SouthWest;
                bishopMasks[sq] = BitOps.AndNot(bishopMasks[sq], edgeSquares);

                rookMasks[sq] = BitOps.AndNot(ray.North, maskRanks[63]) | BitOps.AndNot(ray.South, maskRanks[0]) |
                                BitOps.AndNot(ray.East, maskFiles[7]) | BitOps.AndNot(ray.West, maskFiles[0]);
            }
        }

        private static void InitPieceMagicTables()
        {
            for (int sq = 0; sq < Constants.MAX_SQUARES; ++sq)
            {
                for (int blockerIndex = 0; blockerIndex < (1 << rookIndexBits[sq]); ++blockerIndex)
                {
                    ulong blockers = GetBlockersFromIndex(blockerIndex, rookMasks[sq]);
                    int index = sq * 4096;
                    index += (int)((blockers * rookMagics[sq]) >> (64 - rookIndexBits[sq]));
                    rookTable[index] = GetRookAttacks(sq, blockers);
                }

                for (int blockerIndex = 0; blockerIndex < (1 << bishopIndexBits[sq]); ++blockerIndex)
                {
                    ulong blockers = GetBlockersFromIndex(blockerIndex, bishopMasks[sq]);
                    int index = sq * 1024;
                    index += (int)((blockers * bishopMagics[sq]) >> (64 - bishopIndexBits[sq]));
                    bishopTable[index] = GetBishopAttacks(sq, blockers);
                }
            }
        }

        private static readonly ulong[] rookMagics =
        {
            #region rookMagics data
            0xa8002c000108020ul, 0x6c00049b0002001ul, 0x100200010090040ul, 0x2480041000800801ul, 0x280028004000800ul,
            0x900410008040022ul, 0x280020001001080ul, 0x2880002041000080ul, 0xa000800080400034ul, 0x4808020004000ul,
            0x2290802004801000ul, 0x411000d00100020ul, 0x402800800040080ul, 0xb000401004208ul, 0x2409000100040200ul,
            0x1002100004082ul, 0x22878001e24000ul, 0x1090810021004010ul, 0x801030040200012ul, 0x500808008001000ul,
            0xa08018014000880ul, 0x8000808004000200ul, 0x201008080010200ul, 0x801020000441091ul, 0x800080204005ul,
            0x1040200040100048ul, 0x120200402082ul, 0xd14880480100080ul, 0x12040280080080ul, 0x100040080020080ul,
            0x9020010080800200ul, 0x813241200148449ul, 0x491604001800080ul, 0x100401000402001ul, 0x4820010021001040ul,
            0x400402202000812ul, 0x209009005000802ul, 0x810800601800400ul, 0x4301083214000150ul, 0x204026458e001401ul,
            0x40204000808000ul, 0x8001008040010020ul, 0x8410820820420010ul, 0x1003001000090020ul, 0x804040008008080ul,
            0x12000810020004ul, 0x1000100200040208ul, 0x430000a044020001ul, 0x280009023410300ul, 0xe0100040002240ul,
            0x200100401700ul, 0x2244100408008080ul, 0x8000400801980ul, 0x2000810040200ul, 0x8010100228810400ul,
            0x2000009044210200ul, 0x4080008040102101ul, 0x40002080411d01ul, 0x2005524060000901ul, 0x502001008400422ul,
            0x489a000810200402ul, 0x1004400080a13ul, 0x4000011008020084ul, 0x26002114058042ul
            #endregion
        };

        private static readonly ulong[] bishopMagics =
        {
            #region bishopMagics data
            0x89a1121896040240ul, 0x2004844802002010ul, 0x2068080051921000ul, 0x62880a0220200808ul, 0x4042004000000ul,
            0x100822020200011ul, 0xc00444222012000aul, 0x28808801216001ul, 0x400492088408100ul, 0x201c401040c0084ul,
            0x840800910a0010ul, 0x82080240060ul, 0x2000840504006000ul, 0x30010c4108405004ul, 0x1008005410080802ul,
            0x8144042209100900ul, 0x208081020014400ul, 0x4800201208ca00ul, 0xf18140408012008ul, 0x1004002802102001ul,
            0x841000820080811ul, 0x40200200a42008ul, 0x800054042000ul, 0x88010400410c9000ul, 0x520040470104290ul,
            0x1004040051500081ul, 0x2002081833080021ul, 0x400c00c010142ul, 0x941408200c002000ul, 0x658810000806011ul,
            0x188071040440a00ul, 0x4800404002011c00ul, 0x104442040404200ul, 0x511080202091021ul, 0x4022401120400ul,
            0x80c0040400080120ul, 0x8040010040820802ul, 0x480810700020090ul, 0x102008e00040242ul, 0x809005202050100ul,
            0x8002024220104080ul, 0x431008804142000ul, 0x19001802081400ul, 0x200014208040080ul, 0x3308082008200100ul,
            0x41010500040c020ul, 0x4012020c04210308ul, 0x208220a202004080ul, 0x111040120082000ul, 0x6803040141280a00ul,
            0x2101004202410000ul, 0x8200000041108022ul, 0x21082088000ul, 0x2410204010040ul, 0x40100400809000ul,
            0x822088220820214ul, 0x40808090012004ul, 0x910224040218c9ul, 0x402814422015008ul, 0x90014004842410ul,
            0x1000042304105ul, 0x10008830412a00ul, 0x2520081090008908ul, 0x40102000a0a60140ul
            #endregion
        };

        private static readonly int[] rookIndexBits =
        {
            12, 11, 11, 11, 11, 11, 11, 12,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            12, 11, 11, 11, 11, 11, 11, 12
        };

        private static readonly int[] bishopIndexBits =
        {
            6, 5, 5, 5, 5, 5, 5, 6,
            5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5,
            6, 5, 5, 5, 5, 5, 5, 6
        };
        
        private static readonly ulong[] rookMasks = GC.AllocateArray<ulong>(Constants.MAX_SQUARES, true);
        private static readonly ulong[] bishopMasks = GC.AllocateArray<ulong>(Constants.MAX_SQUARES, true);
        private static readonly ulong[] rookTable = GC.AllocateArray<ulong>(Constants.MAX_SQUARES * 4096, true);
        private static readonly ulong[] bishopTable = GC.AllocateArray<ulong>(Constants.MAX_SQUARES * 1024, true);

        #endregion
    }
}