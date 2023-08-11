using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

public class MyBotOld : IChessBot
{
    readonly static int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    static Random rng = new();
    MovePlan classMovePlan = new();
    static bool playingAsWhite;

    public Move Think(Board board, Timer timer) {
        playingAsWhite = board.IsWhiteToMove;
        classMovePlan = classMovePlan.FindCurrentBranch(board);
        classMovePlan.BuildTree(board, 4);
        classMovePlan = classMovePlan.ChooseBestMove(-10);
        System.Console.WriteLine("This board state has: " + BasicEvalPos(board) + " goodness.");
        System.Console.WriteLine("This move has:        " + classMovePlan.eval + " goodness.");
        return classMovePlan.move;
    }

    //evaluate a move

    //get the basic piece advantage of the current position
    static int BasicEvalPos(Board board) {
        int eval = 0;
        foreach(PieceList pieces in board.GetAllPieceLists()) {
            bool isMyPiece = (pieces.IsWhitePieceList == board.IsWhiteToMove);
            int isMyPieceMultiplier = isMyPiece ? 1 : -1;
            foreach(Piece piece in pieces) {
                int pieceValue = pieceValues[(int)piece.PieceType];
                switch(piece.PieceType) {
                    case PieceType.Pawn:
                        if (piece.IsWhite)
                            pieceValue += piece.Square.Rank * 10;
                        else
                            pieceValue += (7 - piece.Square.Rank) * 10;
                        break;
                    default: break;
                }
                eval += pieceValue * isMyPieceMultiplier;
                
            }
        }
        return eval;
    }

    struct MovePlan {
        public MovePlan() {
            possibleMoves = new List<MovePlan>();
            eval = 0;
            move = Move.NullMove;
        }

        public MovePlan(Board board, int maxTreeDepth) {
            move = Move.NullMove;
            eval = 0;
            possibleMoves = new List<MovePlan>();
            BuildTree(board, maxTreeDepth);
        }

        private MovePlan(Board board, Move move, int maxTreeDepth) {
            this.move = move;
            possibleMoves = new List<MovePlan>();
            board.MakeMove(move);
            eval = BasicEvalPos(board) * (board.IsWhiteToMove == MyBotOld.playingAsWhite ? 1: -1);
            BuildTree(board, maxTreeDepth);
            board.UndoMove(move);
        }

        public void BuildTree(Board board, int maxTreeDepth) {
            if (maxTreeDepth > 0) {
                if(possibleMoves.Count == 0) {
                    foreach(Move currentMove in board.GetLegalMoves()) {
                        possibleMoves.Add(new MovePlan(board, currentMove, maxTreeDepth - 1));
                    }
                } else {
                    foreach(MovePlan currentMove in possibleMoves) {
                        board.MakeMove(currentMove.move);
                        currentMove.BuildTree(board, maxTreeDepth - 1);
                        board.UndoMove(currentMove.move);
                    }
                }
            }
        }

        // Calculate the goodness of a move to the depth calculated. Stop the tree if it's goodness goes below minEval.

        // Returns the best possible outcome from this tree.
        private int EvalTree(int minEval, bool ourTurn) {
            int thisLevelEval = eval;
            if (possibleMoves.Count == 0) return thisLevelEval;
            if (ourTurn) {
                foreach (MovePlan childMovePlan in possibleMoves) {
                    int childEval = childMovePlan.EvalTree(minEval, !ourTurn);
                    if (childEval > thisLevelEval)
                        thisLevelEval = childEval;
                }
            }
            else {
                foreach (MovePlan childMovePlan in possibleMoves) {
                    int childEval = childMovePlan.EvalTree(minEval, !ourTurn);
                    if (childEval < thisLevelEval)
                        thisLevelEval = childEval;
                }
            }
            return thisLevelEval;
        }

        public MovePlan FindCurrentBranch(Board board) {
            if (move == Move.NullMove)
                return this;
            Move enemyMove = board.GameMoveHistory[^1];
            foreach(MovePlan movePlan in possibleMoves)
                if (enemyMove == movePlan.move)
                    return movePlan;
            return new MovePlan(board, 4);
       }

        public MovePlan ChooseBestMove(int minEval) {
            List<int> evals = new();
            foreach (MovePlan childMove in possibleMoves)
                evals.Add(childMove.EvalTree(minEval, true));
            List<int> bestMoves = new();
            int maxEval = evals.Max();
            for (int i = 0; i < evals.Count; i++)
                if (evals[i] == maxEval)
                    bestMoves.Add(i);
            return possibleMoves[bestMoves[rng.Next(bestMoves.Count)]];
        }

        public Move move; // the move this struct represents
        public int eval; // the current evaluation of the goodness of this move
        public List<MovePlan> possibleMoves;  // the list of possible moves (each is another branch in the tree)
    }
}