using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class ChessGameLauncher
    {
        static void Main(string[] args)
        {
            new ChessGame().play();

        }
    }

    class ChessGame
    {
        int turn, turnWhenPawnMoved2Steps;
        bool isPlayerWhite;
        bool check, amIInDanger;
        bool mate;
        bool drawByStalemate;
        bool drawByThreeFoldRepition;
        bool drawBy50MovesRule;
        bool drawByAcceptingDrawSuggestion;
        ChessPiece[,] board;
        ChessPiece[][,] boardsHistory;

        public ChessGame() { }

        public void play()
        {
            turn = 1;
            turnWhenPawnMoved2Steps = -1;
            bool enPassant = false;
            drawByStalemate = false;
            bool valid = false;
            bool threeFoldRepitionApplies = false;
            bool ruleOf50MovesApplies = false;
            Location dest = new Location();
            Location from = new Location();
            Location kingLocation = new Location();
            Location opponentKingLocation = new Location();
            Location pieceDangersKing = new Location();
            bool isCheckingOpponentMovements = false;
            board = new ChessPiece[8, 8];
            boardsHistory = new ChessPiece[50][,];
            Location[] locationsHistory = new Location[100];
            bool[] enPassantHistory = new bool[50];
            ChessPiece chessPieceInDestBeforeMoveMade = new ChessPiece();
            Move playersMove = new Move();
            
            initiallizeBoard(board);
           // stalemateTest(board);
            
            ChessPiece[,] boardCopy = getBoardCopy(board);
            boardsHistory[turn - 1] = boardCopy;
            enPassantHistory[turn-1] = enPassant;

            while (!mate && !drawByStalemate && !drawBy50MovesRule && !drawByThreeFoldRepition && !drawByAcceptingDrawSuggestion)
            {
                if (turn % 2 != 0)
                {
                    isPlayerWhite = true;
                }
                else
                {
                    isPlayerWhite = false;
                }

                kingLocation.findMyKingLocation(board, isPlayerWhite);
                opponentKingLocation.findOpponentKingLocation(board, isPlayerWhite);

                drawByStalemate = isStalemate(board, from, dest, kingLocation, pieceDangersKing, isCheckingOpponentMovements, locationsHistory, chessPieceInDestBeforeMoveMade, enPassantHistory, enPassant);
                if (drawByStalemate)
                {
                    Console.WriteLine();
                    Console.WriteLine("Stalemate! game ended. its a draw!");
                    Console.WriteLine();
                    break;
                }
                mate = isMate(board, from, dest, kingLocation, pieceDangersKing, isCheckingOpponentMovements, locationsHistory, chessPieceInDestBeforeMoveMade, enPassant);
                if (mate)
                {
                    Console.WriteLine();
                    Console.WriteLine("Mate! {0} player won!", isPlayerWhite?"black":"white");
                    Console.WriteLine();
                    break;
                }
                if (turn >= 50)
                    ruleOf50MovesApplies = isDrawBy50MovesRule(board);
                if (ruleOf50MovesApplies)
                {
                    Console.WriteLine();
                    Console.WriteLine("The 50 moves rule now applies. you can demend a draw by entering (R)");
                    Console.WriteLine();
                }
                
                printBoard(board);

                Console.WriteLine();
                Console.WriteLine("turn:" + turn);
                Console.WriteLine("{0} player please enter a move: " +
                    ((threeFoldRepitionApplies || ruleOf50MovesApplies) ? "" : " you can always suggest a draw by entering (S)"), (isPlayerWhite ? "white" : "black"));
                string input = Console.ReadLine().Trim();
                getPlayersMoveInput(from,dest, input, ruleOf50MovesApplies, threeFoldRepitionApplies, valid);

                if (drawByAcceptingDrawSuggestion || drawBy50MovesRule || drawByThreeFoldRepition)
                    break;

                amIInDanger = amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant);

                bool PawnMoved2steps = false;
                if (board[from.row, from.col] is Pawn)
                    PawnMoved2steps = ((Pawn)board[from.row, from.col]).isPawnMoved2Steps(board, from, dest, isPlayerWhite);
                if (PawnMoved2steps)
                    turnWhenPawnMoved2Steps = turn;
                enPassant = isEnPassant(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, turnWhenPawnMoved2Steps);

                move(board, from, dest, pieceDangersKing, boardsHistory, turn, kingLocation, isCheckingOpponentMovements, amIInDanger, locationsHistory, enPassantHistory, enPassant);


                if ((board[from.row, from.col] is EmptyChessPiece) && (!(board[dest.row, dest.col] is EmptyChessPiece))) // אם בוצע מהלך
                {
                    boardCopy = getBoardCopy(board);
                    boardsHistory[turn] = boardCopy;
                    locationsHistory[turn * 2 - 2] = from;
                    locationsHistory[turn * 2 - 1] = dest;
                    enPassantHistory[turn] = enPassant;

                    check = isCheck(board, from, opponentKingLocation, pieceDangersKing, isCheckingOpponentMovements, enPassant);
                    if (check)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Check!");
                        Console.WriteLine();
                        pieceDangersKing.row = dest.row;
                        pieceDangersKing.col = dest.col;
                    }

                    if (turn >= 8)
                    {
                        threeFoldRepitionApplies = isThreeFoldRepitionDraw(boardsHistory, board, turn, locationsHistory, from, pieceDangersKing, kingLocation, isCheckingOpponentMovements, enPassantHistory, enPassant);
                        if (threeFoldRepitionApplies)
                        {
                            Console.WriteLine();
                            Console.WriteLine("The threeFold repition rule now applies. you can demend a draw by entering (T)");
                            Console.WriteLine();
                        }
                    }

                    if ((board[dest.row, dest.col].ToString().Equals("WP")) && (dest.row == 0) ||
                            (board[dest.row, dest.col].ToString().Equals("BP")) && (dest.row == 7))
                    {
                        string chessPieceChosen = choosePieceToPromotePawn(); // הכתרה
                        board[dest.row, dest.col] = promotePawn(dest, chessPieceChosen, isPlayerWhite);
                    }

                    turn++;
                }
            }

            Console.ReadLine();
        }

        public void initiallizeBoard(ChessPiece[,] board)
        {
            Bishop queenAsBlackBishop = new Bishop(false), queenAsWhiteBishop = new Bishop(true);
            Rook queenAsBlackRook = new Rook(false, false), queenAsWhiteRook = new Rook(true, false);
            board[0, 0] = new Rook(false, false); board[0, 1] = new Knight(false); board[0, 2] = new Bishop(false); board[0, 3] = new Queen(false, queenAsBlackBishop, queenAsBlackRook);
            board[0, 4] = new King(false, false); board[0, 5] = new Bishop(false); board[0, 6] = new Knight(false); board[0, 7] = new Rook(false, false);
            for (int i = 0; i < 8; i++)
            {
                board[1, i] = new Pawn(false, false);
                board[2, i] = new EmptyChessPiece();
                board[3, i] = new EmptyChessPiece();
                board[4, i] = new EmptyChessPiece();
                board[5, i] = new EmptyChessPiece();
                board[6, i] = new Pawn(true, false);
            }
            board[7, 0] = new Rook(true, false); board[7, 1] = new Knight(true); board[7, 2] = new Bishop(true); board[7, 3] = new Queen(true, queenAsWhiteBishop, queenAsWhiteRook);
            board[7, 4] = new King(true, false); board[7, 5] = new Bishop(true); board[7, 6] = new Knight(true); board[7, 7] = new Rook(true, false);
        }

        public void printBoard(ChessPiece[,] board)
        {
            Console.WriteLine();
            Console.Write("   A  B  C  D  E  F  G  H ");
            Console.WriteLine();
            for (int i = 0, num = 8; i < 8 && num > 0; i++, num--)
            {
                Console.Write(num + " ");
                for (int j = 0; j < 8; j++)
                {
                    Console.Write(board[i, j].ToString() + " ");
                }
                Console.WriteLine();
            }
        }

        public void getPlayersMoveInput(Location from, Location dest, string input, bool ruleOf50MovesApplies, bool threeFoldRepitionApplies, bool valid)
        {
            valid = isInputValid(input, turn, ruleOf50MovesApplies, threeFoldRepitionApplies);
            while (!valid)
            {
                Console.WriteLine("Invalid input, please enter a valid move:");
                input = Console.ReadLine().Trim();
                valid = isInputValid(input, turn, ruleOf50MovesApplies, threeFoldRepitionApplies);
            }

            if ((input[0] == 'S' || input[0] == 's') && input.Length == 1)
            {
                   drawByAcceptingDrawSuggestion = suggestDraw(isPlayerWhite);
                   if (drawByAcceptingDrawSuggestion)
                       return;
                   if (!drawByAcceptingDrawSuggestion)
                   {
                       printBoard(board);
                       Console.WriteLine("{0} player please enter a move: " +
                       ((threeFoldRepitionApplies || ruleOf50MovesApplies) ? "" : " you can always suggest a draw by entering (S)"), (isPlayerWhite ? "white" : "black"));
                       input = Console.ReadLine().Trim();
                       getPlayersMoveInput(from, dest, input, ruleOf50MovesApplies, threeFoldRepitionApplies, valid);
                   }
            }

            if (ruleOf50MovesApplies && input.Length == 1 && (input[0] == 'R' || input[0] == 'r'))
            {
                drawBy50MovesRule = true;
                Console.WriteLine("{0} player demends a draw due to the 50 moves rule.", (isPlayerWhite ? "white" : "black"));
                Console.WriteLine("game ended. its a draw!");
                return;
            }

            if (threeFoldRepitionApplies && input.Length == 1 && (input[0] == 'T' || input[0] == 't'))
            {
                drawByThreeFoldRepition = true;
                Console.WriteLine("{0} player demends a draw due to the threeFold repition rule.", (isPlayerWhite ? "white" : "black"));
                Console.WriteLine("game ended. its a draw!");
                return;
            }

            if (valid && input.Length == 4)
            {
                castingInputToPositionOnBoard(input, from, dest);
            }
        }

        public bool isInputValid(string input, int turn, bool ruleOf50MovesApplies, bool threeFoldRepitionApplies)
        {
            string validRowInput = "12345678";
            string validColumnInput = "ABCDEFGHabcdefgh";

            bool valid = true;

            int begin = 0, end = input.Length - 1;
            if (begin > end)
            {
                valid = false;
            }
            if (end > 3 || end == 1 || end == 2)
            {
                valid = false;
            }
            if (end == 0)
            {
                if ((turn >= 8 && threeFoldRepitionApplies) || (turn >= 50 && ruleOf50MovesApplies))
                {
                    if (threeFoldRepitionApplies)
                    {
                        if (input[0] != 'T' && input[0] != 't')
                            valid = false;
                    }
                    if (ruleOf50MovesApplies)
                    {
                        if (input[0] != 'R' && input[0] != 'r')
                            valid = false;
                    }
                }
                else
                {
                    if (input[0] != 'S' && input[0] != 's')
                        valid = false;
                }
            }

            if (end == 3)
            {

                if (!(validRowInput.Contains(input[1]) && validRowInput.Contains(input[3])))
                    valid = false;


                if (!(validColumnInput.Contains(input[0]) && validColumnInput.Contains(input[2])))
                    valid = false;

            }

            return valid;
        }

        public void castingInputToPositionOnBoard(string input, Location from, Location dest)
        {
            string startPositionStr = "" + input[0] + input[1];
            string endPositionStr = "" + input[2] + input[3];
            string startPosition = castingColumnToPosition(startPositionStr);
            string endPosition = castingColumnToPosition(endPositionStr);
             
            dest.row = int.Parse("" + endPosition[1]);
            dest.col = int.Parse("" + endPosition[0]);
            dest.row = castingRowToPosition(dest.row);
         
            from.row = int.Parse("" + startPosition[1]);
            from.col = int.Parse("" + startPosition[0]);
            from.row = castingRowToPosition(from.row);
        } 

        public string castingColumnToPosition(string columnLetter) // H to 7
        {
            string convertedPosition = "";
            switch (columnLetter[0])
            {
                case 'A':
                    convertedPosition += "0";
                    break;
                case 'a':
                    convertedPosition += "0";
                    break;
                case 'B':
                    convertedPosition += "1";
                    break;
                case 'b':
                    convertedPosition += "1";
                    break;
                case 'C':
                    convertedPosition += "2";
                    break;
                case 'c':
                    convertedPosition += "2";
                    break;
                case 'D':
                    convertedPosition += "3";
                    break;
                case 'd':
                    convertedPosition += "3";
                    break;
                case 'E':
                    convertedPosition += "4";
                    break;
                case 'e':
                    convertedPosition += "4";
                    break;
                case 'F':
                    convertedPosition += "5";
                    break;
                case 'f':
                    convertedPosition += "5";
                    break;
                case 'G':
                    convertedPosition += "6";
                    break;
                case 'g':
                    convertedPosition += "6";
                    break;
                case 'H':
                    convertedPosition += "7";
                    break;
                case 'h':
                    convertedPosition += "7";
                    break;
            }
            convertedPosition += columnLetter[1];
            return convertedPosition;
        }

        public int castingRowToPosition(int row) // 8 to 0
        {
            switch (row)
            {
                case 1:
                    row = 7;
                    break;
                case 2:
                    row = 6;
                    break;
                case 3:
                    row = 5;
                    break;
                case 4:
                    row = 4;
                    break;
                case 5:
                    row = 3;
                    break;
                case 6:
                    row = 2;
                    break;
                case 7:
                    row = 1;
                    break;
                case 8:
                    row = 0;
                    break;
            }
            return row;
        }

        public bool isEnPassant(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, int turnWhenPawnMoved2Steps)
        {
            bool moved2steps;
            bool canPawnEnPassant = false;
            Location pawnToEat = new Location();

            if (board[from.row, from.col] is Pawn)
            {
                //moved2steps = ((Pawn)board[from.row, from.col]).isPawnMoved2Steps(board, from, dest, isPlayerWhite); // checks if the piece moved 2 steps
                moved2steps = ((Pawn)board[from.row, from.col]).getIsMoved2Steps();
                if (moved2steps)
                {
                    pawnToEat.row = dest.row;
                    pawnToEat.col = dest.col;
                }
                if (moved2steps == false)
                {
                    if (!(board[from.row, from.col] is EmptyChessPiece))
                        ((Pawn)board[from.row, from.col]).setIsMoved2Steps(false);
                }
                if (turn == turnWhenPawnMoved2Steps + 2)
                {
                    moved2steps = false;
                    canPawnEnPassant = false;
                    if ((!(board[pawnToEat.row, pawnToEat.col] is EmptyChessPiece)) && board[pawnToEat.row, pawnToEat.col] is Pawn)
                        ((Pawn)board[pawnToEat.row, pawnToEat.col]).setIsMoved2Steps(false);

                }
                else if (turn == turnWhenPawnMoved2Steps + 1)
                    canPawnEnPassant = ((Pawn)board[from.row, from.col]).canPawnDoEnPassant(board, from, dest, isPlayerWhite);

            }
            return canPawnEnPassant;
        }

        public ChessPiece[,] doEnPassantMove(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant)
        {
            if (enPassant && isPlayerWhite)
            {
                if (from.row > 0)
                {
                    if (dest.row == from.row - 1)
                    {
                        if (from.col > 0)
                        {
                            if ((board[from.row, from.col - 1] is Pawn) && (dest.col == from.col - 1))
                            {
                                board[from.row, from.col - 1] = new EmptyChessPiece();
                                return board;
                            }
                        }
                        if (from.col < 7)
                        {
                            if ((board[from.row, from.col + 1] is Pawn) && (dest.col == from.col + 1))
                            {
                                board[from.row, from.col + 1] = new EmptyChessPiece();
                                return board;
                            }
                        }
                    }
                }
            }

            if (enPassant && (!isPlayerWhite))
            {
                if (from.row < 7)
                {
                    if (dest.row == from.row + 1)
                    {
                        if (from.col > 0)
                        {
                            if ((board[from.row, from.col - 1] is Pawn) && (dest.col == from.col - 1))
                            {
                                board[from.row, from.col - 1] = new EmptyChessPiece();
                                return board;
                            }
                        }
                        if (from.col < 7)
                        {
                            if ((board[from.row, from.col + 1] is Pawn) && (dest.col == from.col + 1))
                            {
                                board[from.row, from.col + 1] = new EmptyChessPiece();
                                return board;
                            }
                        }
                    }
                }
            }
            return board;
        }

        public string choosePieceToPromotePawn()
        {
            string ChessPieceChosen = "";
            
            Console.WriteLine("Congratulations! you can now promote your pawn to another piece!");
            Console.WriteLine("Please choose which piece you would like to promote your pawn to:");
            Console.WriteLine("Q- Queen, R- Rook, N- kNight, B- Bishop P- Pawn");
            ChessPieceChosen = "" + Console.ReadLine().Trim();

            bool choiseValid = isPieceChoiceValid(ChessPieceChosen);
            while (!choiseValid)
            {
                Console.WriteLine("Invalid input, please enter a valid letter from the list:");
                ChessPieceChosen = "" + Console.ReadLine().Trim();
                choiseValid = isPieceChoiceValid(ChessPieceChosen);
            }      
            return ChessPieceChosen;
        }

        bool isPieceChoiceValid(string input)
        {
            bool valid = true;
            string validChoise = "QRBNPqrbnp";

            if (input.Length != 1)
                valid = false;

            if (!(validChoise.Contains(input)))
            {
                valid = false;
            }

            return valid;
        }

        ChessPiece promotePawn(Location dest, string chessPieceChosen, bool isPlayerWhite)
        {
            ChessPiece newChessPieceAfterPromotion = new ChessPiece();
            bool isNewPieceWhite = false;
            if (isPlayerWhite)
                isNewPieceWhite = true;

            Bishop queenAsBishop = new Bishop(isNewPieceWhite);
            Rook queenAsRook = new Rook(isNewPieceWhite, true);

            switch (chessPieceChosen)
            {
                case "Q":
                case "q":
                    newChessPieceAfterPromotion = new Queen(isNewPieceWhite, queenAsBishop, queenAsRook);
                    break;
                case "R":
                case "r":
                    newChessPieceAfterPromotion = new Rook(isNewPieceWhite, true);
                    break;
                case "N":
                case "n":
                    newChessPieceAfterPromotion = new Knight(isNewPieceWhite);
                    break;
                case "B":
                case "b":
                    newChessPieceAfterPromotion = new Bishop(isNewPieceWhite);
                    break;
                case "P":
                case "p":
                    newChessPieceAfterPromotion = new Pawn(isNewPieceWhite, false);
                    break;
            }
            return newChessPieceAfterPromotion;
        }

        public ChessPiece[,] move(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, ChessPiece[][,] boardsHistory, int turn, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger, Location[] locationsHistory, bool[] enPassantHistory, bool enPassant)
        {
            bool isCastlingRightOk = false;
            bool isCastlingLeftOk = false;
            ChessPiece chessPieceInDestBeforeMoveMade = new ChessPiece();

            if (board[from.row, from.col] is King && ((King)board[from.row, from.col]).getIsMovedFromStartPosition().Equals(false))
            {
                isCastlingRightOk = ((King)board[from.row, from.col]).isCastlingRightPossible(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                isCastlingLeftOk = ((King)board[from.row, from.col]).isCastlingLeftPossible(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
            }

            if (board[from.row, from.col].isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
            {
                if((!(board[dest.row,dest.col] is EmptyChessPiece)))
                {
                    chessPieceInDestBeforeMoveMade = board[dest.row, dest.col];
                }

                if (isCastlingRightOk)
                {
                    board[dest.row, dest.col] = board[from.row, from.col];
                    board[from.row, from.col] = new EmptyChessPiece();
                    board[from.row, from.col + 1] = board[from.row, from.col + 3];
                    board[from.row, from.col + 3] = new EmptyChessPiece();
                    Console.WriteLine();
                    Console.WriteLine("castling!");
                    Console.WriteLine();
                    return board;
                }
                if (isCastlingLeftOk)
                {
                    board[dest.row, dest.col] = board[from.row, from.col];
                    board[from.row, from.col] = new EmptyChessPiece();
                    board[from.row, from.col - 1] = board[from.row, from.col - 4];
                    board[from.row, from.col - 4] = new EmptyChessPiece();
                    Console.WriteLine();
                    Console.WriteLine("castling!");
                    Console.WriteLine();
                    return board;
                }
                if (enPassant)
                    doEnPassantMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant);

                if ((!(board[from.row, from.col] is EmptyChessPiece)))
                {
                    if (board[dest.row, dest.col] is EmptyChessPiece)
                    {
                        board[dest.row, dest.col] = board[from.row, from.col];
                        board[from.row, from.col] = new EmptyChessPiece();

                        if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                        {
                            board[from.row, from.col] = board[dest.row, dest.col];
                            board[dest.row, dest.col] = new EmptyChessPiece();
                            return board;
                        }
                        board[from.row, from.col] = board[dest.row, dest.col];
                        board[dest.row, dest.col] = new EmptyChessPiece();

                    }
                    else
                    {
                        ChessPiece temp = board[from.row, from.col];
                        ChessPiece temp2 = board[dest.row, dest.col];
                        board[dest.row, dest.col] = temp;
                        board[from.row, from.col] = new EmptyChessPiece();
                        if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                        {
                            board[from.row, from.col] = temp;
                            board[dest.row, dest.col] = temp2;
                            return board;

                        }
                        board[from.row, from.col] = temp;
                        board[dest.row, dest.col] = temp2;
                    }
                }

                board[dest.row, dest.col] = board[from.row, from.col];
                board[from.row, from.col] = new EmptyChessPiece();

                if (board[dest.row, dest.col] is King)
                   ((King)board[dest.row, dest.col]).setIsMovedFromStartPosition(true);
                if (board[dest.row, dest.col] is Rook)
                    ((Rook)board[dest.row, dest.col]).setIsMovedFromStartPosition(true);
            }
            else
            {
                Console.WriteLine("Ilegal move. please enter a legal move.");
            }
            return board;
        }

        public ChessPiece[,] getBoardCopy(ChessPiece[,] board)
        {
            ChessPiece[,] boardCopy = new ChessPiece[8, 8];
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    boardCopy[row, col] = board[row, col].copy();
                }

            return boardCopy;
        }

        public bool suggestDraw(bool isPlayerWhite)
        {
            string input;
            bool valid = false;

            Console.WriteLine();
            Console.WriteLine("{0} player suggests a draw. {1} player, please enter (Y) to accept "
                + "or (N) to decline and continue the game:", isPlayerWhite ? "white" : "black", isPlayerWhite ? "black" : "white");
            input = Console.ReadLine().Trim();

            while (!valid)
            {
                valid = true;

                if (input.Length != 1)
                    valid = false;

                if ((input != "Y") && (input != "y") && (input != "N") && (input != "n"))
                    valid = false;

                if (valid)
                    break;
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Invalid input. please enter (Y) to accept the draw suggestion or (N) to decline.");
                    Console.WriteLine();
                    input = Console.ReadLine().Trim();
                }
            }

            if ((input == "Y") || (input == "y"))
            {
                Console.WriteLine();
                Console.WriteLine("{0} player accepts the draw suggestion. Its a draw!", isPlayerWhite ? "black" : "white");
                return true;
            }

            if ((input == "N") || (input == "n"))
            {
                Console.WriteLine();
                Console.WriteLine("{0} player declines the draw suggestion. the game continues", isPlayerWhite ? "black" : "white");
                return false;
            }

            return false;
        }

        public bool isThreeFoldRepitionDraw(ChessPiece[][,] boardsHistory, ChessPiece[,] board, int turn, Location[] locationsHistory, Location from, Location pieceDangersKing, Location kingLocation, bool isCheckingOpponentMovements, bool[] enPassantHistory, bool enPassant)
        {
            bool threeFoldApplies = false;
            ChessPiece[,] first = new ChessPiece[8, 8];
            ChessPiece[,] second = new ChessPiece[8, 8];
            ChessPiece[,] third = new ChessPiece[8, 8];

            int firstPosition = 0;
            int secondPosition = 0;
            int thirdPosition = 0;
            bool positionsRepeat = false;
            bool turnRepeat = false;
            bool enPassantRepeat = false;
            bool enPassant1 = false, enPassant2 = false, enPassant3 = false;
            bool castlingRepeat = false, castlingRightPossible1 = false, castlingRightPossible2 = false, castlingRightPossible3 = false;
            bool castlingLeftPossible1 = false, castlingLeftPossible2 = false, castlingLeftPossible3 = false;


            int z = 0;
            int i = 0;
            bool same = false;
            int count = 0;

            if (turn >= 8)
            {
                while (positionsRepeat == false && z < boardsHistory.Length - 2)
                {
                    count = 0;
                    for (i = z + 1; i < boardsHistory.Length; i++)
                    {
                        same = objArrayEquallity(boardsHistory[z], boardsHistory[i], turn);
                        if (same == false)
                            continue;
                        if (same)
                        {
                            count++;
                            if (count == 1)
                            {
                                firstPosition = z;
                                secondPosition = i;
                                first = boardsHistory[z];
                                second = boardsHistory[i];
                            }
                            if (count == 2)
                            {
                                thirdPosition = i;
                                third = boardsHistory[i];
                            }
                        }
                    }

                    if (count >= 2)
                    {
                        positionsRepeat = true;
                        if ((firstPosition % 2 == 0 && secondPosition % 2 == 0 && thirdPosition % 2 == 0) ||
                            (firstPosition % 2 != 0 && secondPosition % 2 != 0 && thirdPosition % 2 != 0))
                        {
                            turnRepeat = true;
                        }

                        if(board[from.row,from.col] is King)
                        {
                            castlingRightPossible1 = ((King)board[from.row, from.col]).isCastlingRightPossible(first, locationsHistory[firstPosition * 2], locationsHistory[firstPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                            castlingLeftPossible1 = ((King)board[from.row, from.col]).isCastlingLeftPossible(first, locationsHistory[firstPosition * 2], locationsHistory[firstPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                        castlingRightPossible2 = ((King)board[from.row, from.col]).isCastlingRightPossible(second, locationsHistory[secondPosition * 2], locationsHistory[secondPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                        castlingLeftPossible2 = ((King)board[from.row, from.col]).isCastlingLeftPossible(second, locationsHistory[secondPosition * 2], locationsHistory[secondPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                        castlingRightPossible3 = ((King)board[from.row, from.col]).isCastlingRightPossible(third, locationsHistory[thirdPosition * 2], locationsHistory[thirdPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                        castlingLeftPossible3 = ((King)board[from.row, from.col]).isCastlingLeftPossible(third, locationsHistory[thirdPosition * 2], locationsHistory[thirdPosition * 2 + 1], pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger);
                        }
                        enPassant1 = enPassantHistory[firstPosition];
                        enPassant2 = enPassantHistory[secondPosition];
                        enPassant3 = enPassantHistory[thirdPosition];
                    }
                    z++;
                }              

                if (((castlingRightPossible1 && castlingRightPossible2 && castlingRightPossible3) && (castlingLeftPossible1 && castlingLeftPossible2 && castlingLeftPossible3))
                    || ((castlingRightPossible1 == false && castlingRightPossible2 == false && castlingRightPossible3 == false) && (castlingLeftPossible1 == false && castlingLeftPossible2 == false && castlingLeftPossible3 == false)))
                   castlingRepeat = true;

                if ((enPassant1 && enPassant2 && enPassant3) ||
                    (enPassant1 == false && enPassant2 == false && enPassant3 == false))
                    enPassantRepeat = true;

                if (positionsRepeat && turnRepeat && castlingRepeat && enPassantRepeat)
                    threeFoldApplies = true;
            } 
            return threeFoldApplies;
        }

        public bool objArrayEquallity(ChessPiece[,] first, ChessPiece[,] second, int turn)
        {
            bool arraysEqual = false;

            if (turn >= 6 && first != null && second != null)
            {
                if (first.Length == second.Length)
                {
                    for (int a = 0; a < 8; a++)
                    {
                        for (int b = 0; b < 8; b++)
                        {
                            if ((first[a, b] != null && second[a, b] == null) || (first[a, b] == null && second[a, b] != null))
                                return false;
                            if (first[a, b] == null && second[a, b] == null)
                                arraysEqual = true;
                            if (first[a, b] != null && second[a, b] != null)
                            {
                                if (first[a, b].ToString().Equals(second[a, b].ToString()))
                                    arraysEqual = true;
                                else
                                    return false;

                            }
                        }
                    }
                }
            }
            return arraysEqual;
        }
        
        public bool isDrawBy50MovesRule(ChessPiece[,] board)
        {
            bool pawnsNotMoved = false;
            bool piecesEaten = false;

            for (int i = 0; i < 8; i++)
            {
                if ((!(board[1, i] is EmptyChessPiece)) && board[1, i] is Pawn)
                    pawnsNotMoved = true;
                if (board[6, i] != null && board[6, i] is Pawn)
                    pawnsNotMoved = true;
                if (pawnsNotMoved == false)
                    break;
            }

            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] != null)
                        count++;
                }
            }

            if (count < 32)
            {
                piecesEaten = true;
            }

            if ((pawnsNotMoved == true) && (piecesEaten == false))
            {
                Console.WriteLine();
                Console.WriteLine("The 50 moves rule now applies. you can demend a draw by entering (R)");
                Console.WriteLine();
                return true;
            }
        return false;
        }

        public bool isCheck(ChessPiece[,] board, Location from, Location kingLocation, Location pieceDangersKing, bool isCheckingOpponentMovements, bool enPassant)
        {
            if(board[from.row,from.col] is King)
                kingLocation.findMyKingLocation(board, isPlayerWhite);
            Location temp = new Location();
            temp.row = from.row;
            temp.col = from.col;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    from.row = row;
                    from.col = col;
                    if ((isPlayerWhite && board[from.row, from.col].getIsWhite().Equals(true)) || ((!isPlayerWhite) && board[from.row, from.col].getIsWhite().Equals(false)))
                    {
                        if (board[from.row, from.col].isLegalMove(board, from, kingLocation, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            from.row = temp.row;
                            from.col = temp.col;
                            return true;
                        }
                    }
                        from.row = temp.row;
                        from.col = temp.col;
                }
            return false;
        }

        public bool amIInCheck(ChessPiece[,] board, Location from, Location kingLocation, Location pieceDangersKing, bool isCheckingOpponentMovements, Location dest, bool enPassant)
        {
            isCheckingOpponentMovements = true;
            kingLocation.findMyKingLocation(board, isPlayerWhite);
            Location temp = new Location();
            temp.row = from.row;
            temp.col = from.col;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    from.row = row;
                    from.col = col;
                    if ((isPlayerWhite && board[from.row, from.col].getIsWhite().Equals(false)) || ((!isPlayerWhite) && board[from.row, from.col].getIsWhite().Equals(true)))
                    {
                        if (board[from.row, from.col].isLegalMove(board, from, kingLocation, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            from.row = temp.row;
                            from.col = temp.col;
                            return true;
                        }
                    }
                        from.row = temp.row;
                        from.col = temp.col;      
                }
            isCheckingOpponentMovements = false;
            return false;
        }

        public void stalemateTest(ChessPiece[,] board)
        {
            board[4,2] = board[6,2];
            board[6, 2] = new EmptyChessPiece();
            board[3, 7] = board[1, 7];
            board[1, 7] = new EmptyChessPiece();
            board[4, 7] = board[6, 7];
            board[6, 7] = new EmptyChessPiece();
            board[3, 0] = board[1, 0];
            board[1, 0] = new EmptyChessPiece();
            board[4, 0] = board[7, 3];
            board[7, 3] = new EmptyChessPiece();
            board[2, 0] = board[0, 0];
            board[0, 0] = new EmptyChessPiece();
            board[3, 0] = board[4, 0];
            board[4, 0] = new EmptyChessPiece();
            board[2, 7] = board[2, 0];
            board[2, 0] = new EmptyChessPiece();
            board[1, 2] = board[3, 0];
            board[3, 0] = new EmptyChessPiece();
            board[2, 5] = board[1, 5];
            board[1, 5] = new EmptyChessPiece();
            board[1, 3] = board[1, 2];
            board[1, 2] = new EmptyChessPiece();
            board[1, 5] = board[0, 4];
            board[0, 4] = new EmptyChessPiece();
            board[1, 1] = board[1, 3];
            board[1, 3] = new EmptyChessPiece();
            board[5, 3] = board[0, 3];
            board[0, 3] = new EmptyChessPiece();
            board[0, 1] = board[1, 1];
            board[1, 1] = new EmptyChessPiece();
            board[1, 7] = board[5, 3];
            board[5, 3] = new EmptyChessPiece();
            board[0, 2] = board[0, 1];
            board[0, 1] = new EmptyChessPiece();
            board[2, 6] = board[1, 5];
            board[1, 5] = new EmptyChessPiece();
            Console.WriteLine();
            Console.WriteLine("For stalemate, move: C8 to E6");
            Console.WriteLine();
        }

        public bool isStalemate(ChessPiece[,] board, Location from, Location dest, Location kingLocation, Location pieceDangersKing, bool isCheckingOpponentMovements, Location[] locationsHistory, ChessPiece chessPieceInDestBeforeMoveMade, bool[] enPassantHistory, bool enPassant)
        {
            Location temp = new Location();
            Location tempDest = new Location();
            temp.row = from.row;
            temp.col = from.col;
            tempDest.row = dest.row;
            tempDest.col = dest.col;

            if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                return false;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    from.row = row;
                    from.col = col;
                    for (int destRow = 0; destRow < 8; destRow++)
                        for (int destCol = 0; destCol < 8; destCol++)
                        {
                            dest.row = destRow;
                            dest.col = destCol;
                            if ((isPlayerWhite && board[from.row, from.col].getIsWhite().Equals(true)) || ((!isPlayerWhite) && board[from.row, from.col].getIsWhite().Equals(false)))
                            {
                                if (board[from.row, from.col].isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                                {
                                    if (!((board[from.row, from.col] is EmptyChessPiece)) && enPassant == false)
                                    {
                                        if ((board[dest.row, dest.col] is EmptyChessPiece))
                                        {
                                            board[dest.row, dest.col] = board[from.row, from.col];
                                            board[from.row, from.col] = new EmptyChessPiece();

                                            if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                                            {
                                                board[from.row, from.col] = board[dest.row, dest.col];
                                                board[dest.row, dest.col] = new EmptyChessPiece();
                                                drawByStalemate = true;
                                            }
                                            else
                                            {
                                                board[from.row, from.col] = board[dest.row, dest.col];
                                                board[dest.row, dest.col] = new EmptyChessPiece();
                                                drawByStalemate = false;
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            ChessPiece tempFrom = board[from.row, from.col];
                                            ChessPiece tempDest2 = board[dest.row, dest.col];
                                            board[dest.row, dest.col] = tempFrom;
                                            board[from.row, from.col] = new EmptyChessPiece();
                                            if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                                            {
                                                board[from.row, from.col] = tempFrom;
                                                board[dest.row, dest.col] = tempDest2;
                                                drawByStalemate = true;
                                            }
                                            else
                                            {
                                                board[from.row, from.col] = tempFrom;
                                                board[dest.row, dest.col] = tempDest2;
                                                drawByStalemate = false;
                                                return false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                }
            from.row = temp.row;
            from.col = temp.col;
            dest.row = tempDest.row;
            dest.col = tempDest.col;

            if (!drawByStalemate)
                return false;
            else
            {
                printBoard(board);
                return true;
            }
        }

        public bool isMate(ChessPiece[,] board, Location from, Location dest, Location kingLocation, Location pieceDangersKing, bool isCheckingOpponentMovements, Location[] locationsHistory, ChessPiece chessPieceInDestBeforeMoveMade, bool enPassant)
        {
            Location temp = new Location();
            Location tempDest = new Location();
            temp.row = from.row;
            temp.col = from.col;
            tempDest.row = dest.row;
            tempDest.col = dest.col;

            if (!amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                return false;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    from.row = row;
                    from.col = col;
                    for (int destRow = 0; destRow < 8; destRow++)
                        for (int destCol = 0; destCol < 8; destCol++)
                        {
                            dest.row = destRow;
                            dest.col = destCol;
                            if ((isPlayerWhite && board[from.row, from.col].getIsWhite().Equals(true)) || ((!isPlayerWhite) && board[from.row, from.col].getIsWhite().Equals(false)))
                            {
                                if (board[from.row, from.col].isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                                {
                                    if (!((board[from.row, from.col] is EmptyChessPiece)) && enPassant == false)
                                    {
                                        if ((board[dest.row, dest.col] is EmptyChessPiece))
                                        {
                                            board[dest.row, dest.col] = board[from.row, from.col];
                                            board[from.row, from.col] = new EmptyChessPiece();

                                            if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                                            {
                                                board[from.row, from.col] = board[dest.row, dest.col];
                                                board[dest.row, dest.col] = new EmptyChessPiece();
                                                mate = true;
                                            }
                                            else
                                            {
                                                board[from.row, from.col] = board[dest.row, dest.col];
                                                board[dest.row, dest.col] = new EmptyChessPiece();
                                                mate = false;
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            ChessPiece tempFrom = board[from.row, from.col];
                                            ChessPiece tempDest2 = board[dest.row, dest.col];
                                            board[dest.row, dest.col] = tempFrom;
                                            board[from.row, from.col] = new EmptyChessPiece();
                                            if (amIInCheck(board, from, kingLocation, pieceDangersKing, isCheckingOpponentMovements, dest, enPassant))
                                            {
                                                board[from.row, from.col] = tempFrom;
                                                board[dest.row, dest.col] = tempDest2;
                                                mate = true;
                                            }
                                            else
                                            {
                                                board[from.row, from.col] = tempFrom;
                                                board[dest.row, dest.col] = tempDest2;
                                                mate = false;
                                                return false;
                                            }
                                        }
                                    }
                                }
                            }  
                        }           
                }
            from.row = temp.row;
            from.col = temp.col;
            dest.row = tempDest.row;
            dest.col = tempDest.col;

            if (!mate)
                return false;
            else
            {
                printBoard(board);
                return true;
            }
        }
    }

    class Location
    {
        public int row;
        public int col;

        public Location findMyKingLocation(ChessPiece[,] board, bool isPlayerWhite)
        {
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (isPlayerWhite)
                    {
                        if (board[row, col].ToString().Equals("WK"))
                        {
                            this.row = row;
                            this.col = col;
                            break;
                        }
                    }
                    if (!isPlayerWhite)
                    {
                        if (board[row, col].ToString().Equals("BK"))
                        {
                            this.row = row;
                            this.col = col;
                            break;
                        }
                    }
                }
            return this;
        }

        public Location findOpponentKingLocation(ChessPiece[,] board, bool isPlayerWhite)
        {
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    if (isPlayerWhite)
                    {
                        if (board[row, col].ToString().Equals("BK"))
                        {
                            this.row = row;
                            this.col = col;
                            break;
                        }
                    }
                    if (!isPlayerWhite)
                    {
                        if (board[row, col].ToString().Equals("WK"))
                        {
                            this.row = row;
                            this.col = col;
                            break;
                        }
                    }
                }
            return this;
        }
    }

    class Move
    {
        Location from;
        Location dest;
    }

    class ChessPiece
    {
        bool isWhite;

        public ChessPiece() { }

        public ChessPiece(bool isWhite)
        {
            setIsWhite(isWhite);
        }

        public bool getIsWhite()
        {
            return isWhite;
        }

        public void setIsWhite(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        public virtual ChessPiece copy()
        {
            ChessPiece result = new ChessPiece(isWhite);
            result.isWhite = this.isWhite;
            return result;
        }

        public virtual bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            bool moveValid = true;

            if (dest.col == from.col && dest.row == from.row) // if tring to stay at the same position
            {
                moveValid = false;
            }
            if (board[from.row, from.col] is EmptyChessPiece) // if current position is empty
            {
                moveValid = false;
            }

            // if trying to move an opponent piece
            if (isCheckingOpponentMovements == false)
            {
                if (!isPlayerWhite)
                {
                    if (!(board[from.row, from.col] is EmptyChessPiece))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            moveValid = false;
                        }
                    }
                }

                if (isPlayerWhite)
                {
                    if (!(board[from.row, from.col] is EmptyChessPiece))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            moveValid = false;
                        }
                    }
                }
            }
            if ((!(board[dest.row, dest.col] is EmptyChessPiece)) && (!(board[from.row, from.col] is EmptyChessPiece))) // if current position & destination is not empty
            {
                if ((board[dest.row, dest.col].getIsWhite().Equals(true) && board[from.row, from.col].getIsWhite().Equals(true)) ||
                    (board[dest.row, dest.col].getIsWhite().Equals(false) && board[from.row, from.col].getIsWhite().Equals(false)))// if piece at dest has the same color as mine
                {
                    moveValid = false;
                }
                else
                {
                    moveValid = true;
                }
            }
            return moveValid;
        }
    }

    class EmptyChessPiece : ChessPiece
    {

        public EmptyChessPiece() { }
        public override string ToString()
        {
            return "  ";
        }

        public override ChessPiece copy()
        {
            EmptyChessPiece result = new EmptyChessPiece();
            return result;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            return false;
        }
    }

    class Pawn : ChessPiece
    {
        bool isMoved2Steps;

        public Pawn(bool isWhite, bool isMoved2Steps) : base(isWhite)
        {
            setIsMoved2Steps(isMoved2Steps);
        }

        public bool getIsMoved2Steps()
        {
            return isMoved2Steps;
        }

        public void setIsMoved2Steps(bool isMoved2Steps)
        {
            this.isMoved2Steps = isMoved2Steps;
        }

        public override ChessPiece copy()
        {
            Pawn result = new Pawn(getIsWhite(), isMoved2Steps);
            result.setIsWhite(this.getIsWhite());
            result.isMoved2Steps = this.isMoved2Steps;
            return result;
        }

        public bool isPawnMoved2Steps(ChessPiece[,] board, Location from, Location dest, bool isPlayerWhite)
        {
            if ((!(board[from.row, from.col] is EmptyChessPiece)) && board[from.row, from.col].getIsWhite().Equals(false))
            {
                if (from.row == 1)
                {
                    if (dest.col == from.col && dest.row == from.row + 2 && board[from.row + 1, from.col] is EmptyChessPiece)
                    {
                        if (dest.col < 7)
                        {
                            if ((!(board[dest.row, dest.col + 1] is EmptyChessPiece)) && board[dest.row, dest.col + 1] is Pawn && ((Pawn)board[dest.row, dest.col + 1]).getIsWhite().Equals(true))
                            {
                                this.setIsMoved2Steps(true);
                                return true;
                            }
                        }
                        if (dest.col > 0)
                        {
                            if ((!(board[dest.row, dest.col - 1] is EmptyChessPiece)) && board[dest.row, dest.col - 1] is Pawn && ((Pawn)board[dest.row, dest.col - 1]).getIsWhite().Equals(true))
                            {
                                this.setIsMoved2Steps(true);
                                return true;
                            }
                        }
                    }
                }
            }


            if ((!(board[from.row, from.col] is EmptyChessPiece)) && board[from.row, from.col].getIsWhite().Equals(true))
            {
                if (from.row == 6)
                {
                    if (dest.col == from.col && dest.row == from.row - 2 && board[from.row - 1, from.col] is EmptyChessPiece)
                    {
                        if (dest.col < 7)
                        {

                            if ((!(board[dest.row, dest.col + 1] is EmptyChessPiece)) && board[dest.row, dest.col + 1] is Pawn && ((Pawn)board[dest.row, dest.col + 1]).getIsWhite().Equals(false))
                            {
                                this.setIsMoved2Steps(true);
                                return true;
                            }
                        }
                        if (dest.col > 0)
                        {
                            if ((!(board[dest.row, dest.col - 1] is EmptyChessPiece)) && board[dest.row, dest.col - 1] is Pawn && ((Pawn)board[dest.row, dest.col - 1]).getIsWhite().Equals(false))
                            {
                                this.setIsMoved2Steps(true);
                                return true;
                            }
                        }
                    }
                }
            } 
            return false;
        }

        public bool canPawnDoEnPassant(ChessPiece[,] board, Location from, Location dest, bool isPlayerWhite)
        {
            if (isPlayerWhite)
            {
                if (from.col > 0)
                {
                    if ((board[from.row, from.col - 1] is Pawn) && ((Pawn)board[from.row, from.col - 1]).getIsWhite().Equals(false) && (dest.row == from.row - 1) && (dest.col == from.col - 1) && ((Pawn)board[from.row, from.col - 1]).getIsMoved2Steps().Equals(true))
                    {
                        return true;
                    }
                }

                if (from.col < 7)
                {
                    if ((board[from.row, from.col + 1] is Pawn) && ((Pawn)board[from.row, from.col + 1]).getIsWhite().Equals(false) && (dest.row == from.row - 1) && (dest.col == from.col + 1) && ((Pawn)board[from.row, from.col + 1]).getIsMoved2Steps().Equals(true))
                    {
                        return true;
                    }
                }
            }

            if (!isPlayerWhite)
            {
                if (from.col > 0)
                {
                    if ((board[from.row, from.col - 1] is Pawn) && ((Pawn)board[from.row, from.col - 1]).getIsWhite().Equals(true) && (dest.row == from.row + 1) && (dest.col == from.col - 1) && ((Pawn)board[from.row, from.col - 1]).getIsMoved2Steps().Equals(true))
                    {
                        return true;
                    }
                }
                if (from.col < 7)
                {
                    if ((board[from.row, from.col + 1] is Pawn) && ((Pawn)board[from.row, from.col + 1]).getIsWhite().Equals(true) && (dest.row == from.row + 1) && (dest.col == from.col + 1) && ((Pawn)board[from.row, from.col + 1]).getIsMoved2Steps().Equals(true))
                    {
                        return true;
                    }
                }
            } 
            return false;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            

            if ((((Pawn)board[from.row, from.col]).getIsWhite().Equals(false)) && (board[dest.row, dest.col] is EmptyChessPiece))
                {
                    if (from.row == 1)
                    {
                        if (dest.col == from.col && dest.row == from.row + 1)
                        {
                            return true;
                        }
                        if (dest.col == from.col && dest.row == from.row + 2 && board[from.row + 1, from.col] is EmptyChessPiece)
                        {
                            return true;
                        }
                    }
                    else if ((from.row < 7) && (dest.col == from.col && dest.row == from.row + 1) &&
                        (board[dest.row, dest.col] is EmptyChessPiece)) 
                    {
                        return true;
                    }
                }
            if ((((Pawn)board[from.row, from.col]).getIsWhite().Equals(true)) && (board[dest.row, dest.col] is EmptyChessPiece))
                {
                    if (from.row == 6)
                    {
                        if (dest.col == from.col && dest.row == from.row - 1)
                        {
                            return true;
                        }
                        if (dest.col == from.col && dest.row == from.row - 2 && board[from.row - 1, from.col] is EmptyChessPiece)
                        {
                            return true;
                        }
                    }
                    else if ((from.row > 0) && (dest.col == from.col && dest.row == from.row - 1) && (board[dest.row, dest.col] is EmptyChessPiece))
                    {
                        return true;
                    }
                }
            // check if pawn can eat
            if ((((Pawn)board[from.row, from.col]).getIsWhite().Equals(true)))
            {
                if (from.row > 0)
                {
                    if (dest.row == from.row - 1)
                    {
                        if (from.col > 0)
                        {
                            if ((!(board[from.row - 1, from.col - 1] is EmptyChessPiece)) && (dest.col == from.col - 1))
                            {
                                return true;
                            }
                        }
                        if (from.col < 7)
                        {
                            if ((!(board[from.row - 1, from.col + 1] is EmptyChessPiece)) && (dest.col == from.col + 1))
                            {
                                return true;
                            }
                        }
                    }
                }

                if (enPassant)
                {
                    if (from.row > 0)
                    {
                        if (dest.row == from.row - 1)
                        {
                            if (from.col > 0)
                            {
                                if ((board[from.row, from.col - 1] is Pawn) && (dest.col == from.col - 1))
                                {
                                    return true;
                                }
                            }
                            if (from.col < 7)
                            {
                                if ((board[from.row, from.col + 1] is Pawn) && (dest.col == from.col + 1))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (board[from.row, from.col].getIsWhite().Equals(false))
            {
                if (from.row < 7)
                {
                    if (dest.row == from.row + 1)
                    {
                        if (from.col > 0)
                        {
                            if ((!(board[from.row + 1, from.col - 1] is EmptyChessPiece)) && (dest.col == from.col - 1))
                            {
                                return true;
                            }
                        }
                        if (from.col < 7)
                        {
                            if ((!(board[from.row + 1, from.col + 1] is EmptyChessPiece)) && (dest.col == from.col + 1))
                            {
                                return true;
                            }
                        }
                    }
                }

                if (enPassant)
                {
                    if (from.row < 7)
                    {
                        if (dest.row == from.row + 1)
                        {
                            if (from.col > 0)
                            {
                                if ((board[from.row, from.col - 1] is Pawn) && (dest.col == from.col - 1))
                                {
                                    return true;
                                }
                            }
                            if (from.col < 7)
                            {
                                if ((board[from.row, from.col + 1] is Pawn) && (dest.col == from.col + 1))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            } 
            return false;
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "P";
        }
    }

    class Knight : ChessPiece
    {
        public Knight(bool isWhite) : base(isWhite)
        {
        }

        public override ChessPiece copy()
        {
            Knight result = new Knight(getIsWhite());
            result.setIsWhite(this.getIsWhite());
            return result;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            if (from.row > 0)
            {
                if (from.col > 1)
                {
                    if (dest.row == from.row - 1 && dest.col == from.col - 2)
                    {
                        return true;
                    }
                }
                if (from.col < 6)
                {
                    if (dest.row == from.row - 1 && dest.col == from.col + 2)
                    {
                        return true;
                    }
                }
            }
            if (from.row < 7)
            {
                if (from.col > 1)
                {
                    if (dest.row == from.row + 1 && dest.col == from.col - 2)
                    {
                        return true;
                    }
                }
                if (from.col < 6)
                {
                    if (dest.row == from.row + 1 && dest.col == from.col + 2)
                    {
                        return true;
                    }
                }
            }
            if (from.row > 1)
            {
                if (from.col > 0)
                {
                    if (dest.row == from.row - 2 && dest.col == from.col - 1)
                    {
                        return true;
                    }
                }
                if (from.col < 7)
                {
                    if (dest.row == from.row - 2 && dest.col == from.col + 1)
                    {
                        return true;
                    }
                }
            }
            if (from.row < 6)
            {
                if (from.col > 0)
                {
                    if (dest.row == from.row + 2 && dest.col == from.col - 1)
                    {
                        return true;
                    }
                }
                if (from.col < 7)
                {
                    if (dest.row == from.row + 2 && dest.col == from.col + 1)
                    {
                        return true;
                    }
                }
            }  
            return false;
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "N";
        }
    }

    class King : ChessPiece
    {
        bool isMovedFromStartPosition;

        public King(bool isWhite, bool isMovedFromStartPosition) : base(isWhite)
        {
            setIsMovedFromStartPosition(isMovedFromStartPosition);
        }

        public bool getIsMovedFromStartPosition()
        {
            return isMovedFromStartPosition;
        }

        public void setIsMovedFromStartPosition(bool isMovedFromStartPosition)
        {
            this.isMovedFromStartPosition = isMovedFromStartPosition;
        }

        public override ChessPiece copy()
        {
            King result = new King(getIsWhite(), isMovedFromStartPosition);
            result.setIsWhite(this.getIsWhite());
            result.isMovedFromStartPosition = this.isMovedFromStartPosition;
            return result;
        }

        public bool isDestInCheck(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {   
                Location temp = new Location();
                temp.row = from.row;
                temp.col = from.col;
                for (int row = 0; row < 8; row++)
                    for (int col = 0; col < 8; col++)
                    {
                        from.row = row;
                        from.col = col;
                        if (!(board[from.row, from.col] is King) && (isPlayerWhite==true))
                        {
                            if (board[from.row, from.col].isLegalMove(board, from, dest, pieceDangersKing, check, false, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            from.row = temp.row;
                            from.col = temp.col;
                            return true;
                        }

                        from.row = temp.row;
                        from.col = temp.col;

                        }
                        if (!(board[from.row, from.col] is King) && (isPlayerWhite == false))
                        {
                            if (board[from.row, from.col].isLegalMove(board, from, dest, pieceDangersKing, check, true, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                from.row = temp.row;
                                from.col = temp.col;
                                return true;
                            }

                            from.row = temp.row;
                            from.col = temp.col;
                        }
                        
                    }
                      return false;  
        }

        public bool isCastlingRightPossible(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            bool castlingPossible = false;
            Location temp = new Location();
            temp.row = dest.row;
            temp.col = dest.col;

            if (isPlayerWhite)
            {
                if ((board[from.row, from.col] is King) && (((King)board[from.row, from.col]).getIsMovedFromStartPosition().Equals(false)) && 
                (((King)board[from.row, from.col]).getIsWhite().Equals(true)) && (board[from.row, from.col + 3] is Rook) && 
                (((Rook)board[from.row, from.col + 3]).getIsMovedFromStartPosition().Equals(false)) && 
                (board[from.row, from.col + 1] is EmptyChessPiece) && (board[from.row, from.col + 2] is EmptyChessPiece) && 
                ((!amIInDanger))) 
                {
                    dest.col = from.col + 1;
                    if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                    {
                        dest.col = from.col + 2;
                        if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                        {
                            castlingPossible = true;
                            dest.col = temp.col;
                        }
                    }
                }
            }
            if (!isPlayerWhite)
            {
                if ((board[from.row, from.col] is King) && (((King)board[from.row, from.col]).getIsMovedFromStartPosition().Equals(false)) && 
                (((King)board[from.row, from.col]).getIsWhite().Equals(false)) && (board[from.row, from.col + 3] is Rook) &&
                (((Rook)board[from.row, from.col + 3]).getIsMovedFromStartPosition().Equals(false)) && 
                (board[from.row, from.col + 1] is EmptyChessPiece) && (board[from.row, from.col + 2] is EmptyChessPiece) && 
                ((!amIInDanger)))
                {
                    dest.col = from.col + 1;
                    if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                    {
                        dest.col = from.col + 2;
                        if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                        {
                            castlingPossible = true;
                            dest.col = temp.col;
                        }
                    }
                }
            }
            return castlingPossible;
        }

        public bool isCastlingLeftPossible(ChessPiece[,] board, Location from, Location dest, Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool enPassant, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            bool castlingPossible = false;
            Location temp = new Location();
            temp.row = dest.row;
            temp.col = dest.col;

            if (isPlayerWhite)
            {
                if ((board[from.row, from.col] is King) && (((King)board[from.row, from.col]).getIsMovedFromStartPosition().Equals(false)) &&
                (((King)board[from.row, from.col]).getIsWhite().Equals(true)) && (board[from.row, from.col - 4] is Rook) &&
                (((Rook)board[from.row, from.col - 4]).getIsMovedFromStartPosition().Equals(false)) &&
                (board[from.row, from.col - 1] is EmptyChessPiece) && (board[from.row, from.col - 2] is EmptyChessPiece) && (board[from.row, from.col - 3] is EmptyChessPiece) &&
                 ((!amIInDanger)))
                {
                    dest.col = from.col - 1;
                    if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                    {
                        dest.col = from.col - 2;
                        if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                        {
                            castlingPossible = true;
                            dest.col = temp.col;
                        }
                    }
                }
            }
            if (!isPlayerWhite)
            {
                if ((board[from.row, from.col] is King) && (((King)board[from.row, from.col]).getIsMovedFromStartPosition().Equals(false)) && 
                (((King)board[from.row, from.col]).getIsWhite().Equals(false)) && (board[from.row, from.col - 4] is Rook) && 
               (((Rook)board[from.row, from.col - 4]).getIsMovedFromStartPosition().Equals(false)) && 
                (board[from.row, from.col - 1] is EmptyChessPiece) && (board[from.row, from.col - 2] is EmptyChessPiece) && (board[from.row, from.col - 3] is EmptyChessPiece) &&
                ((!amIInDanger)))
                {
                    dest.col = from.col - 1;
                    if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                    {
                        dest.col = from.col - 2;
                        if (isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, enPassant, kingLocation, isCheckingOpponentMovements, amIInDanger) == false)
                        {
                            castlingPossible = true;
                            dest.col = temp.col;
                        }
                    }
                }
            }
            return castlingPossible;
        } 

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool hiluhu, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            bool moveAvailable = false;

            if (from.row < 7)
            {
                if ((dest.row == from.row + 1) && (dest.col == from.col))
                {
                    if (board[from.row, from.col].getIsWhite().Equals(true))
                    {
                        if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            moveAvailable = true;
                        }
                        if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col))
                        {
                            moveAvailable = true;
                        }
                    }
                    if (board[from.row, from.col].getIsWhite().Equals(false))
                    {
                        if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            moveAvailable = true;
                        }
                        if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col))
                        {
                            moveAvailable = true;
                        }
                    }
                }
                if (from.col > 0)
                {
                    if ((dest.row == from.row + 1) && (dest.col == from.col - 1))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
                if (from.col < 7)
                {
                    if ((dest.row == from.row + 1) && (dest.col == from.col + 1))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row + 1) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
            }


            if (dest.row == from.row) 
            {
                if (from.col > 0)
                {
                    if (dest.col == from.col - 1)
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
                if (from.col < 7)
                {
                    if (dest.col == from.col + 1)
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
            }

            if (from.row > 0)
            {
                if ((dest.row == from.row - 1) && (dest.col == from.col))
                {
                    if (board[from.row, from.col].getIsWhite().Equals(true))
                    {
                        if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            moveAvailable = true;
                        }
                        if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col))
                        {
                            moveAvailable = true;
                        }
                    }
                    if (board[from.row, from.col].getIsWhite().Equals(false))
                    {
                        if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                        {
                            moveAvailable = true;
                        }
                        if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col))
                        {
                            moveAvailable = true;
                        }
                    }
                }
                if (from.col > 0)
                {
                    if ((dest.row == from.row - 1) && (dest.col == from.col - 1))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col - 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
                if (from.col < 7)
                {
                    if ((dest.row == from.row - 1) && (dest.col == from.col + 1))
                    {
                        if (board[from.row, from.col].getIsWhite().Equals(true))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                        if (board[from.row, from.col].getIsWhite().Equals(false))
                        {
                            if (!isDestInCheck(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                            {
                                moveAvailable = true;
                            }
                            if ((pieceDangersKing.row == from.row - 1) && (pieceDangersKing.col == from.col + 1))
                            {
                                moveAvailable = true;
                            }
                        }
                    }
                }
            }

                if(!amIInDanger)
                {
                    if (dest.row == from.row)
                    {
                        if (from.col > 1)
                        {
                            if (dest.col == from.col - 2)
                            {

                                if (isCastlingLeftPossible(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                                {
                                    moveAvailable = true;
                                }
                            }
                        }
                    }
                    if (dest.row == from.row)
                    {
                        if (from.col < 6)
                        {
                            if (dest.col == from.col + 2) 
                            {

                                if (isCastlingRightPossible(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                                {
                                    moveAvailable = true;
                                }
                            }
                        }
                    }
                }                
            return moveAvailable;
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "K";
        }
    }

    class Rook : ChessPiece
    {
        bool isMovedFromStartPosition;

        public Rook(bool isWhite, bool isMovedFromStartPosition) : base(isWhite)
        {
            setIsMovedFromStartPosition(isMovedFromStartPosition);
        }

        public bool getIsMovedFromStartPosition()
        {
            return isMovedFromStartPosition;
        }

        public void setIsMovedFromStartPosition(bool isMovedFromStartPosition)
        {
            this.isMovedFromStartPosition = isMovedFromStartPosition;
        }

        public override ChessPiece copy()
        {
            Rook result = new Rook(getIsWhite(), isMovedFromStartPosition);
            result.setIsWhite(this.getIsWhite());
            result.isMovedFromStartPosition = this.isMovedFromStartPosition;
            return result;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool hiluhu, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            bool rowUpClear = false;
            bool rowDownClear = false;
            bool columnUpClear = false;
            bool columnDownClear = false;

            if (dest.row < from.row && dest.col == from.col && from.row > 0)
            {
                for (int i = from.row - 1; i >= dest.row; i--)
                {
                    if (board[i, from.col] is EmptyChessPiece || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.row))))
                        rowUpClear = true;
                    else
                    {
                        rowUpClear = false;
                        break;
                    }
                }
            }

            if (dest.row > from.row && dest.col == from.col && from.row < 7)
            {
                for (int i = from.row + 1; i <= dest.row; i++)
                {
                    if (board[i, from.col] is EmptyChessPiece || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.row))))
                        rowDownClear = true;
                    else
                    {
                        rowDownClear = false;
                        break;
                    }
                }
            }

            if (dest.col < from.col && dest.row == from.row && from.col > 0)
            {
                for (int i = from.col - 1; i >= dest.col; i--)
                {
                    if (board[from.row, i] is EmptyChessPiece || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.col))))
                        columnUpClear = true;
                    else
                    {
                        columnUpClear = false;
                        break;
                    }
                }
            }

            if (dest.col > from.col && dest.row == from.row && from.col < 7)
            {
                for (int i = from.col + 1; i <= dest.col; i++)
                {
                    if (board[from.row, i] is EmptyChessPiece || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.col))))
                        columnDownClear = true;
                    else
                    {
                        columnDownClear = false;
                        break;
                    }
                }
            }

            if ((from.row == dest.row || from.col == dest.col) && (rowUpClear == true || rowDownClear == true ||
                    columnUpClear == true || columnDownClear == true))
                return true;
            
            return false;
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "R";
        }
    }

    class Bishop : ChessPiece
    {
        public Bishop(bool isWhite) : base(isWhite)
        {
        }

        public override ChessPiece copy()
        {
            Bishop result = new Bishop(getIsWhite());
            result.setIsWhite(this.getIsWhite());
            return result;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool hiluhu, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            bool upRightClear = false;
            bool upLeftClear = false;
            bool downRightClear = false;
            bool downLeftClear = false;

            if (dest.row < from.row && dest.col > from.col && from.row > 0 && from.col < 7 && from.row - dest.row == dest.col - from.col)
            {
                for (int i = from.row - 1, j = from.col + 1; i >= dest.row && j <= dest.col; i--, j++)
                {
                    if ((board[i, j] is EmptyChessPiece) || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.row && j == dest.col))))
                        upRightClear = true;
                    else
                    {
                        upRightClear = false;
                        break;
                    }
                }
            }

            if (dest.row < from.row && dest.col < from.col && from.row > 0 && from.col > 0 && from.row - dest.row == from.col - dest.col)
            {
                for (int i = from.row - 1, j = from.col - 1; i >= dest.row && j >= dest.col; i--, j--)
                {
                    if ((board[i, j] is EmptyChessPiece) || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.row && j == dest.col))))
                        upLeftClear = true;
                    else
                    {
                        upLeftClear = false;
                        break;
                    }
                }
            }

            if (dest.row > from.row && dest.col > from.col && from.row < 7 && from.col < 7 && dest.row - from.row == dest.col - from.col)
            {
                for (int i = from.row + 1, j = from.col + 1; i <= dest.row && j <= dest.col; i++, j++)
                {
                    if ((board[i, j] is EmptyChessPiece) || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i == dest.row && j == dest.col))))
                        downRightClear = true;
                    else
                    {
                        downRightClear = false;
                        break;
                    }
                }
            }

            if (dest.row > from.row && dest.col < from.col && from.row < 7 && from.col > 0 && dest.row - from.row == from.col - dest.col)
            {
                for (int i = from.row + 1, j = from.col - 1; i <= dest.row && j >= dest.col; i++, j--)
                {
                    if ((board[i, j] is EmptyChessPiece) || ((!(board[dest.row, dest.col] is EmptyChessPiece) && (i==dest.row && j == dest.col))))
                        downLeftClear = true;
                    else
                    {
                        downLeftClear = false;
                        break;
                    }
                }
            }

            if (((dest.row < from.row && dest.col > from.col && from.row - dest.row == dest.col - from.col) ||
                    (dest.row < from.row && dest.col < from.col && from.row - dest.row == from.col - dest.col) ||
                    (dest.row > from.row && dest.col > from.col && dest.row - from.row == dest.col - from.col) ||
                    (dest.row > from.row && dest.col < from.col && dest.row - from.row == from.col - dest.col)) &&
                    (upRightClear == true || upLeftClear == true || downRightClear == true || downLeftClear == true))
                return true;

            return false;
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "B";
        }
    }

    class Queen : ChessPiece
    {
        Bishop queenAsBishop;
        Rook queenAsRook;

        public Queen(bool isWhite, Bishop queenAsBishop, Rook queenAsRook) : base(isWhite)
        {
            setQueenAsBishop(queenAsBishop);
            setQueenAsRook(queenAsRook);
        }

        public Bishop getQueenAsBishop()
        {
            return queenAsBishop;
        }

        public void setQueenAsBishop(Bishop queenAsBishop)
        {
            this.queenAsBishop = queenAsBishop;
        }

        public Rook getQueenAsRook()
        {
            return queenAsRook;
        }

        public void setQueenAsRook(Rook queenAsRook)
        {
            this.queenAsRook = queenAsRook;
        }

        public override ChessPiece copy()
        {
            Queen result = new Queen(getIsWhite(), queenAsBishop, queenAsRook);
            result.setIsWhite(this.getIsWhite());
            result.queenAsBishop = (Bishop)this.queenAsBishop.copy();
            result.queenAsRook = (Rook)this.queenAsRook.copy();
            return result;
        }

        public override bool isLegalMove(ChessPiece[,] board, Location from, Location dest,
            Location pieceDangersKing, bool check, bool isPlayerWhite, int turn, bool hiluhu, Location kingLocation, bool isCheckingOpponentMovements, bool amIInDanger)
        {
            if (!base.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger))
                return false;

            return ((queenAsBishop.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger)) ||
                (queenAsRook.isLegalMove(board, from, dest, pieceDangersKing, check, isPlayerWhite, turn, hiluhu, kingLocation, isCheckingOpponentMovements, amIInDanger)));
        }

        public override string ToString()
        {
            return (getIsWhite() ? "W" : "B") + "Q";
        }
    }
}
