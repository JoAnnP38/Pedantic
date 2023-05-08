// ***********************************************************************
// Assembly         : Pedantic.Chess
// Author           : JoAnn D. Peeler
// Created          : 01-17-2023
//
// Last Modified By : JoAnn D. Peeler
// Last Modified On : 03-27-2023
// ***********************************************************************
// <copyright file="BoardFancy.cs" company="Pedantic.Chess">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary>
//     Partial class that initializes the lookup tables for Fancy Magic
//     bitboard implementation.
// </summary>
// ***********************************************************************
using Pedantic.Utilities;
using System.Numerics;
using System.Runtime.CompilerServices;
using Pedantic.Collections;

namespace Pedantic.Chess
{
    public sealed partial class Board
    {
        static readonly UnsafeArray<ulong> fancyLookupTable = new (88507, true);

        private static ulong RelevantBishopSee(int x, int y)
        {
            // result attacks bitboard
            ulong attacks = 0ul;

            // init ranks & files
            int r, f;

            // Mask relevant bishop occupancy bits
            for (r = y + 1, f = x + 1; r <= 6 && f <= 6; r++, f++) attacks |= (1ul << (r * 8 + f));
            for (r = y - 1, f = x + 1; r >= 1 && f <= 6; r--, f++) attacks |= (1ul << (r * 8 + f));
            for (r = y + 1, f = x - 1; r <= 6 && f >= 1; r++, f--) attacks |= (1ul << (r * 8 + f));
            for (r = y - 1, f = x - 1; r >= 1 && f >= 1; r--, f--) attacks |= (1ul << (r * 8 + f));

            // return attack map
            return attacks;
        }

        private static ulong RelevantRookSee(int x, int y)
        {
            // result attacks bitboard
            ulong attacks = 0ul;

            // init ranks & files
            int r, f;

            // Mask relevant rook occupancy bits
            for (r = y + 1; r <= 6; r++) attacks |= (1ul << (r * 8 + x));
            for (r = y - 1; r >= 1; r--) attacks |= (1ul << (r * 8 + x));
            for (f = x + 1; f <= 6; f++) attacks |= (1ul << (y * 8 + f));
            for (f = x - 1; f >= 1; f--) attacks |= (1ul << (y * 8 + f));

            // return attack map
            return attacks;
        }
        private readonly struct FancyHash
        {
            public readonly int Offset;
            public readonly ulong Mask;
            public readonly ulong Hash;

            public FancyHash(int offset, ulong mask, ulong hash)
            {
                Offset = offset;
                Mask = mask;
                Hash = hash;
            }
        }
        private static readonly UnsafeArray<FancyHash> fancyBishopMagics = new (Constants.MAX_SQUARES)
        {
            new FancyHash( 66157, 18428694421974023679ul, 1187473109101317119ul ),
            new FancyHash( 71730, 18446673567257459711ul, 9223336714375004157ul ),
            new FancyHash( 37781, 18446743798293722623ul, 288441550701068800ul ),
            new FancyHash( 21015, 18446744072633576447ul, 1170795303134035968ul ),
            new FancyHash( 47590, 18446744073671530495ul, 13853037273714524160ul ),
            new FancyHash( 835, 18446744065051963391ul, 2648129775020802048ul ),
            new FancyHash( 23592, 18446741857371152383ul, 578730278520913668ul ),
            new FancyHash( 30599, 18446176691079331839ul, 1155182238468407424ul ),
            new FancyHash( 68776, 18437719247841787903ul, 18420565800289023999ul ),
            new FancyHash( 19959, 18428694421974024191ul, 593981333727348737ul ),
            new FancyHash( 21783, 18446673567257329663ul, 288653413114708096ul ),
            new FancyHash( 64836, 18446743798259908607ul, 306245323880337408ul ),
            new FancyHash( 23417, 18446744063976144895ul, 2310347158529769472ul ),
            new FancyHash( 66724, 18446741857366966271ul, 1187261314343337984ul ),
            new FancyHash( 74542, 18446176691079348223ul, 9188469001234153344ul ),
            new FancyHash( 67266, 18445609308449144831ul, 578171627018125376ul ),
            new FancyHash( 26575, 18442231660775734783ul, 9222949822267379705ul ),
            new FancyHash( 67543, 18437719247841917951ul, 9223020191524333565ul ),
            new FancyHash( 24409, 18428694421940729343ul, 2306265224900983809ul ),
            new FancyHash( 30779, 18446673558600936447ul, 4647151869788945290ul ),
            new FancyHash( 17384, 18446741581957421055ul, 2314815028390789136ul ),
            new FancyHash( 18778, 18446176690007683071ul, 18437719281702141935ul ),
            new FancyHash( 65109, 18445609308453330943ul, 9223363241302802431ul ),
            new FancyHash( 20184, 18444474543197110271ul, 9221115838862475263ul ),
            new FancyHash( 38240, 18444487867259288575ul, 1090988818544ul ),
            new FancyHash( 16459, 18442231660809025535ul, 9223230887045427193ul ),
            new FancyHash( 17432, 18437719239318433791ul, 9223090009958119421ul ),
            new FancyHash( 81040, 18428692205904059903ul, 4570867472252272639ul ),
            new FancyHash( 84946, 18446106185164110847ul, 292734589976199165ul ),
            new FancyHash( 18276, 18445609034107058175ul, 2305878434105819152ul ),
            new FancyHash( 8512, 18444474544268767231ul, 612472197655543809ul ),
            new FancyHash( 78544, 18442205014827982847ul, 2265045493362695ul ),
            new FancyHash( 19974, 18445615974745634815ul, 553992241216ul ),
            new FancyHash( 23850, 18444487875781718015ul, 276996120608ul ),
            new FancyHash( 11056, 18442229478797074431ul, 27023813833162784ul ),
            new FancyHash( 68019, 18437151933931044863ul, 14989104351765610624ul ),
            new FancyHash( 85965, 18427559794152570367ul, 1152957239137415242ul ),
            new FancyHash( 80524, 18444404311622941695ul, 9205355920559695872ul ),
            new FancyHash( 38221, 18442205289172170751ul, 1188932777689485200ul ),
            new FancyHash( 64647, 18437666504634789887ul, 9191846633066727416ul ),
            new FancyHash( 61320, 18446181115098558463ul, 279189160064ul ),
            new FancyHash( 67281, 18445618156487565311ul, 139594580032ul ),
            new FancyHash( 79076, 18443929280722223103ul, 279198499008ul ),
            new FancyHash( 17115, 18441114487701372927ul, 4503601791385728ul ),
            new FancyHash( 50718, 18435484901701451775ul, 1152921764453941296ul ),
            new FancyHash( 24659, 18424225731840835071ul, 2307531983360360472ul ),
            new FancyHash( 38291, 18437736736746896383ul, 18446743798562688988ul ),
            new FancyHash( 30605, 18428729399784241151ul, 68988172306ul ),
            new FancyHash( 37759, 18446741857371152383ul, 576460886529573376ul ),
            new FancyHash( 4639, 18446739641032753151ul, 594475220056658440ul ),
            new FancyHash( 21759, 18446733009332731903ul, 576460752563503233ul ),
            new FancyHash( 67799, 18446721936374366207ul, 1125900041076608ul ),
            new FancyHash( 22841, 18446699801153110015ul, 576460756600479808ul ),
            new FancyHash( 66689, 18446656078352351231ul, 603338863802321408ul ),
            new FancyHash( 62548, 18446708820483505663ul, 18446742973123131421ul ),
            new FancyHash( 66597, 18446673567257459711ul, 18428729606210057999ul ),
            new FancyHash( 86749, 18446176691079331839ul, 1152921573842288770ul ),
            new FancyHash( 69558, 18445609308449144831ul, 9203950263191257135ul ),
            new FancyHash( 61589, 18443911593243705343ul, 9188469139741638527ul ),
            new FancyHash( 62533, 18441076915902087167ul, 18442803424035078081ul ),
            new FancyHash( 64387, 18435410299260502015ul, 610242147571982367ul ),
            new FancyHash( 26581, 18424217262266253311ul, 26177172852973578ul ),
            new FancyHash( 76355, 18437719247841787903ul, 594615890450845658ul ),
            new FancyHash( 11140, 18428694421974023679ul, 1152922330840178696ul ),
        };

        private static readonly UnsafeArray<FancyHash> fancyRookMagics = new (Constants.MAX_SQUARES)
        {
            new FancyHash( 10890, 18446461494909402753ul, 9234631121814487039ul ),
            new FancyHash( 56054, 18446178916109254019ul, 6916402019615277055ul ),
            new FancyHash( 67495, 18445613758508956549ul, 18442234976255475711ul ),
            new FancyHash( 72797, 18444483443308361609ul, 13511417407733898ul ),
            new FancyHash( 17179, 18442222812907171729ul, 13512448205127728ul ),
            new FancyHash( 63978, 18437701552104791969ul, 9008441047646240ul ),
            new FancyHash( 56650, 18428659030500032449ul, 13511211211554864ul ),
            new FancyHash( 15929, 18410573987290513281ul, 18421974885628248056ul ),
            new FancyHash( 55905, 18446461494909370879ul, 9205348825003917308ul ),
            new FancyHash( 26301, 18446178916109222911ul, 21992397144066ul ),
            new FancyHash( 78100, 18445613758508926463ul, 26389411528776ul ),
            new FancyHash( 86245, 18444483443308333567ul, 9223345648611360696ul ),
            new FancyHash( 75228, 18442222812907147775ul, 18446691290705821615ul ),
            new FancyHash( 31661, 18437701552104776191ul, 26391501865056ul ),
            new FancyHash( 38053, 18428659030500033023ul, 18446717683547242472ul ),
            new FancyHash( 37433, 18410573987290513919ul, 26389090795544ul ),
            new FancyHash( 74747, 18446461494901210879ul, 13510901978890243ul ),
            new FancyHash( 53847, 18446178916101258751ul, 844476478521343ul ),
            new FancyHash( 70952, 18445613758501223423ul, 18446181089329217527ul ),
            new FancyHash( 49447, 18444483443301152767ul, 9205920450792660991ul ),
            new FancyHash( 62629, 18442222812901011455ul, 18446462461260324863ul ),
            new FancyHash( 58996, 18437701552100728831ul, 8939786031088007199ul ),
            new FancyHash( 36009, 18428659030500163583ul, 2323998178495432720ul ),
            new FancyHash( 21230, 18410573987290644479ul, 288371136597606656ul ),
            new FancyHash( 51882, 18446461492812250879ul, 18049583150530568ul ),
            new FancyHash( 11841, 18446178914062433791ul, 4505798785105924ul ),
            new FancyHash( 25794, 18445613756529245183ul, 18446180024110022647ul ),
            new FancyHash( 49689, 18444483441462867967ul, 18356529144533024761ul ),
            new FancyHash( 63400, 18442222811330113535ul, 13835059154257051616ul ),
            new FancyHash( 33958, 18437701551064604671ul, 2308130543272738823ul ),
            new FancyHash( 21991, 18428659030533586943ul, 13833926657740128127ul ),
            new FancyHash( 45618, 18410573987324067839ul, 578702106657038400ul ),
            new FancyHash( 70134, 18446460958038490879ul, 2305852801742274608ul ),
            new FancyHash( 75944, 18446178392123244031ul, 581969641496ul ),
            new FancyHash( 68392, 18445613251702815743ul, 1112398102552ul ),
            new FancyHash( 66472, 18444482970861959167ul, 4611686574627225624ul ),
            new FancyHash( 23236, 18442222409180246015ul, 36169744961110010ul ),
            new FancyHash( 19067, 18437701285816819711ul, 4611712960757628934ul ),
            new FancyHash( 0, 18428659039089967103ul, 18446743523949527039ul ),
            new FancyHash( 43566, 18410573995880447999ul, 140874929406016ul ),
            new FancyHash( 29810, 18446324055955930879ul, 2305845220786317312ul ),
            new FancyHash( 65558, 18446044775690665471ul, 18446737474441966591ul ),
            new FancyHash( 77684, 18445484016136879103ul, 276144592896ul ),
            new FancyHash( 73350, 18444362497029306367ul, 2305843214839435264ul ),
            new FancyHash( 61765, 18442119458814160895ul, 18446743455209082877ul ),
            new FancyHash( 49282, 18437633382383869951ul, 13832788662204502015ul ),
            new FancyHash( 78840, 18428661229523288063ul, 68853737476ul ),
            new FancyHash( 82904, 18410576186313768959ul, 18445618103606814718ul ),
            new FancyHash( 24594, 18411277122820570879ul, 5669358141480ul ),
            new FancyHash( 9513, 18411838968950554111ul, 571239694356ul ),
            new FancyHash( 29012, 18412399711257099263ul, 9223372221542596648ul ),
            new FancyHash( 27684, 18413521195870189567ul, 4611686156948013096ul ),
            new FancyHash( 27901, 18415764165096370175ul, 8646911422261395496ul ),
            new FancyHash( 61477, 18420250103548731391ul, 103093927960ul ),
            new FancyHash( 25719, 18429221980453453823ul, 1769914688190546000ul ),
            new FancyHash( 50020, 18411136937243934719ul, 2306924928660668456ul ),
            new FancyHash( 41547, 9367204646130482943ul, 18446730325249744830ul ),
            new FancyHash( 4750, 9511037255406190079ul, 4611688767357457345ul ),
            new FancyHash( 6014, 9654587285881748479ul, 13799028157677500191ul ),
            new FancyHash( 41529, 9941687346832865279ul, 18446181121465744750ul ),
            new FancyHash( 84192, 10515887468735098879ul, 17221764906077391870ul ),
            new FancyHash( 33433, 11664287712539566079ul, 12673129420131140610ul ),
            new FancyHash( 8555, 13961088200148500479ul, 17182358461140924414ul ),
            new FancyHash( 1009, 9331317138511593471ul, 562962977269890ul ),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetRookAttacksFancy(int square, ulong blockers)
        {
            return fancyLookupTable[FancyRookIndex(square, blockers)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetBishopAttacksFancy(int square, ulong blockers)
        {
            return fancyLookupTable[FancyBishopIndex(square, blockers)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetQueenAttacksFancy(int square, ulong blockers)
        {
            return fancyLookupTable[FancyRookIndex(square, blockers)] |
                   fancyLookupTable[FancyBishopIndex(square, blockers)];
        }

        private static void InitFancyMagic()
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int sq = y * 8 + x;
                    ulong rookMask = RelevantRookSee(x, y);
                    ulong bishopMask = RelevantBishopSee(x, y);

                    int cnt = BitOperations.PopCount(rookMask);
                    for (ulong i = 0; i < (ulong)(1 << cnt); i++)
                    {
                        ulong blockers = BitOps.ParallelBitDeposit(i, rookMask);
                        fancyLookupTable[FancyRookIndex(sq, blockers)] = GetRookAttacks(sq, blockers);
                    }

                    cnt = BitOperations.PopCount(bishopMask);
                    for (ulong i = 0; i < (ulong)(1 << cnt); i++)
                    {
                        ulong blockers = BitOps.ParallelBitDeposit(i, bishopMask);
                        fancyLookupTable[FancyBishopIndex(sq, blockers)] = GetBishopAttacks(sq, blockers);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FancyRookIndex(int square, ulong blockers)
        {
            FancyHash m = fancyRookMagics[square];
            return m.Offset + (int)(((blockers | m.Mask) * m.Hash) >> (64 - 12));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FancyBishopIndex(int square, ulong blockers)
        {
            FancyHash m = fancyBishopMagics[square];
            return m.Offset + (int)(((blockers | m.Mask) * m.Hash) >> (64 - 9));
        }
    }
}
