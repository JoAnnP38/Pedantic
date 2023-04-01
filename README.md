## <p style="text-align: center">The Pedantic Chess Engine</p>
<p style="text-align: center;">
/pɪ'dæntɪk/ - [pe·dan·tic]<br />
<span style="font-weight: bold">pedantic</span>
</p>

Pedantic is a chess engine designed to play standard chess via one of the many UCI chess clients (i.e. [Arena](http://www.playwitharena.de/), [BanksiaGUI](https://banksiagui.com/) or [Cute Chess](https://cutechess.com/) for instance.) Any client, GUI, analysis tool designed to work with engines that support the UCI protocol should be able to work with Pedantic. As such, Pedantic is not designed to be used alone to play chess, but it does support some command-line functionality (more on that later.)
### Goals
Pedantic is my first step in advancing my knowledge of computer chess to the point of competing against some of the strongest chess engines int he world. I chose C# as the development language due to my familiarity and it's usefulness over C++ to flesh out my initial ideas. When I have fully explored the latest and cutting edge techniques in computer chess, I hope to eventually port Pedantic to a more performant language such as C++ or perhaps Rust. Throughout my journey of developing Pedantic, I am often reminded of basketball legend, Michael Jordan's quote, "you reach, I teach." However, in this case as I _reach_ to explore the chess programming body-of-work, Pedantic _teaches_ me along every captivating step. 
### Design and Architecture
#### Environment
* Windows 11
* Microsoft Visual Studio Community 2022
* C# / .NET 7.0 (and soon to be released .NET 8.0
* LiteDB NoSQL Database for Storing Trained Weights
#### Structure
* Multi-threaded, command-line, console application
* UCI protocol for interoperability with chess clients
* Opening book library in Polyglot binary format
#### Functionality
* Standard Chess
* Perft Testing
* Label Training Data
* Learning (Logistical Regression)
* Maintain Eval Weights Database 
### Chess/AI Algorithms
#### Search
* Principal Variation/Negamax Search
* Quiesence Search
* Aspiration Window
* Iterative Deepening
* Null Move Pruning
* Razoring
* Futility Pruning
* Late Move Reduction
* Check Extension
* Transposition Tables (search, evaluation, pawn structure)
* Pondering
* Magic Bitboard Move Generation
#### Machine Learning
* Transform PGN Files into Labeled Training Data
* Random Sample Selection from Training Data (currently 30 million positions and growing)
* Evaluation weight tuning using Texel inspired local search
* Logistic Regression using Local Search 
* Performance Enhanced Regression (16X faster than simple Texel)
* Tuned weights automatically written to weights database
### Command-Line Description
    Description:
        The pedantic chess engine.

    Usage: 
        Pedantic [command] [options]

    Options:
        --version       Show version information
        -?, -h, --help  Show help and usage information

    Commands:
        uci      Start the pedantic application in UCI mode.
        perft    Run a standard Perft test.
        label    Pre-process and label PGN data.
        learn    Optimize evaluation function using training data.
        weights  Manipulate the weight database.

    Description:
        Start the pedantic application in UCI mode.

    Usage:
        Pedantic uci [options]

    Options:
        --input <input> Specify a file to read UCI commands from.
        --error <error> Output errors to specified file.
        --random        If specified, adds small random amounts to positional evaluations. [default: false]
        -?, -h, --help  Show help and usage information

    Description:
        Run a standard Perft test.

    Usage:
        Pedantic perft [options]

    Options:
        --type <Average|Details|Normal> Specifies the perft variant to execute. [default: Normal]
        --depth <depth>                 Specifies the maximum depth for the perft test. [default: 6]
        --fen <fen>                     Specifies the starting position if other than the default. 
        -?, -h, --help                  Show help and usage information

    Description:
        Pre-process and label PGN data.

    Usage:
        Pedantic label [options]

    Options:
        --pgn <pgn>         Specifies a PGN input file.
        --data <data>       Specifies the name of the comma-delimited, output file.
        --maxpos <maxpos>   Specify the maximum positions to output. [default: 8000000]
        -?, -h, --help      Show help and usage information

    Description:
        Optimize evaluation function using training data.

    Usage:
        Pedantic learn [options]

    Options:
        --data <data>       The name of the labeled data file.
        --sample <sample>   Specify the number of samples to use from the training data.
        --iter <iter>       Specify the maximum number of iterations before a solution is declared. [default: 200]
        --preserve          When specified intermediate versions of the solution will be saved.
        -?, -h, --help      Show help and usage information

    Description:
        Manipulate the weight database.

    Usage:
        Pedantic weights [options]

    Options:
        --immortal <immortal>   Specifies the OID of a new set of weights to be made the immortal.
        --display <display>     Specifies the OID of a set of weights to be displayed.
        -?, -h, --help          Show help and usage information
### Labeling Training Data
Creating training data involves reading in a specially prepared PGN file that contains 1 or more games. Typically, this could include millions of games. To prepare a PGN for labeling it must first be processed by command-line utility [pgn-extract](https://www.cs.kent.ac.uk/people/staff/djb/pgn-extract/) in such a manner as to clean up incorrect file formatting and to convert the PGN movetext from SAN format to the alebraic notation specified by UCI. And example of pgn-extract usage is shown below:

    pgn-extract -Wuci -oclean.pgn -D -s -C -N -V -tstartpos.txt --fixtagstrings -w128 --minmoves 8 --nobadresults example.pgn

After processing the output created by pgn-extract with Pedantic's label command, a comma-delimited file will be created that looks like the following:

    Hash,Ply,GamePly,FEN,Result
    7AD41D2CC8E1829E,8,135,rnbqk2r/ppp1ppbp/3p1np1/8/3PPP2/2N5/PPP3PP/R1BQKBNR w KQkq - 1 5,0.5
    A1B0F1DAA007E3A3,9,135,rnbqk2r/ppp1ppbp/3p1np1/8/3PPP2/2N2N2/PPP3PP/R1BQKB1R b KQkq - 2 5,0.5
    AB181C6D755E0A48,10,135,rnbq1rk1/ppp1ppbp/3p1np1/8/3PPP2/2N2N2/PPP3PP/R1BQKB1R w KQ - 3 6,0.5
    FE15BDCF4CDEC83B,11,135,rnbq1rk1/ppp1ppbp/3p1np1/8/3PPP2/2NB1N2/PPP3PP/R1BQK2R b KQ - 4 6,0.5

This training data file can then be used to train Pedantic.
### Learning (Processing Training Data)
One you have a training data set with preferably millions of positions (i.e. data file lines/rows) you can use this file to optimize Pedantic's evaluation function by deriving new weights/coefficients for the evaluation function. The the learning (i.e. regression) completes the optimized data will be written to the weights database stored in the common application data folder (i.e. typically this would be C:\ProgramData\Pedantic\Pedantic.db). This file will be created when Pedantic is first run on a system and populated with the default set of weights. Additionally, the program will print the object ID (i.e. OID) of the new set of weights which in turn can be used to configure Pedantic to use the new set of weights. This can be accomplished using one of two methods:
1. Use the Pedantic _weights_ command to replace the current _immortal_ (i.e. default weights) using the given OID .
2. Specify the UCI option Evaluation_ID using the OID of the new weights. You can return to using the default by blanking out this option.
### History
While my first attempt at developing a chess engine took place over 40 years ago in the early 1980s, it was never something I was proud of except as being the motivation to learn C and to sweeten my resume. It was buggy, slow, and only with a lot of luck and a bit of prayer could it successfully complete a game without crashing. It did, however, inspire my interest in computer chess, artificial intelligence and a bet made in 1968 by International Chess Master David Levy that no chess computer would be able to defeat him in a chess match over the next ten years. To make a long story short, Mr. Levy won his bet by defeating all challengers ending with his win over CHESS 4.7 in 1978. However, like with all machines the advancement of computer chess has been relentless. Computer chess programs started beating top level players by the end of of the 1980s and in 1997, a program by IBM named Deep Blue defeated the world champion and Grandmaster, Garry Kasparov. Today, computer chess programs are no longer measured against human competitors because they are simply too strong. Instead, softare developers continue to advanced the science by competing, program against program to achieve the aclaim of strongest chess engine in the world. With Pedantic, I humbly endeavor to join this elite group of developers by learning all the algorithms, heuristics and minutia so that I can create my own strong chess engine. 
### Credits
#### Web Sites
* [Chess Programming Wiki](https://www.chessprogramming.org/Main_Page)  
Inspired everything and continues to inspire!
#### Other Chess Engines
* [Minimal Chess](https://github.com/lithander/MinimalChessEngine)  
Inspired UCI protocol, time control, transposition table aging and engine organization.
* [CPW Engine](https://github.com/nescitus/cpw-engine)  
Inspired implementatin of search, razoring and quiesence.
* [Crafty](https://craftychess.com/)  
Inspired late move reduction and null move pruning technique.
#### Books and Articles
Caplinger, Noah. _Chess Algorithms_. USA: Lulu Press, 2021.

Jordan, Bill. _Advanced Chess Programming_. Orlando: Independently Published, 2022.

Jordan, Bill. _How to Write a Bitboard Chess Engine_. Orlando: Independently Published: 2020.

Jordan, Bill. _Teach a Chess Program Strategy_. Orlando: Independently Published: 2022.

Levy, David. _Computer Gamesmanship_. London: Century Publishing, 1983.

Levy, David. _The Joy of Computer Chess_. Englewood Cliffs: Prentice-Hall, 1984.

Levy, David and Monty NewBorn. _How Computers Play Chess_. New York and Tokyo: Ishi Press International, 2009.

Slate, David and Larry Atkin. "Chess 4.5: The Northwestern University Chess Program." _Computer Chess Compendium_. London: B. T. Batsford, 1988.
    
