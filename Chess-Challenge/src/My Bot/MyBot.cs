using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
    bool white;

    public Move Think(Board board, Timer timer)
    {
        if(board.IsWhiteToMove) {
            white = true;
        }
        System.Console.WriteLine("Current Eval" + BasicEvalPos(board));
        MovePlan ideas = new MovePlan(board, 1);
        return board.GetLegalMoves()[0];
    }

    //evaluate a move

    //get the basic piece advantage of the current position
    static int BasicEvalPos(Board board) {
        int eval = 0;
        foreach(PieceList pieces in board.GetAllPieceLists()) {
            foreach(Piece piece in pieces) {
                if(piece.IsWhite) {
                    eval += pieceValues[(int)piece.PieceType];
                } else {
                    eval -= pieceValues[(int)piece.PieceType];
                }
            }
        }
        return eval;
    }

    struct MovePlan {
        public MovePlan(Board board, int maxTreeDepth) {
            this.board = board;
            move = Move.NullMove;
            depth = 0;
            eval = BasicEvalPos(board);
            possibleMoves = new List<MovePlan>();
            BuildTree(maxTreeDepth);
        }

        private static Board CopyBoard(Board board) {
            return board;
        }


        private MovePlan(MovePlan parent, Move move, int maxTreeDepth) {
            this.move = move;
            board = CopyBoard(parent.board);
            System.Console.WriteLine(move);
            board.MakeMove(move);
            depth = parent.depth + 1;
            eval = BasicEvalPos(board);
            possibleMoves = new List<MovePlan>();
            BuildTree(maxTreeDepth);
        }

        void BuildTree(int maxTreeDepth) {
            if (maxTreeDepth > 0) {
                possibleMoves = new List<MovePlan>();
                foreach(Move currentMove in board.GetLegalMoves()) {
                    possibleMoves.Add(new MovePlan(this, currentMove, maxTreeDepth - 1));
                }
                System.Console.WriteLine(board.AllPiecesBitboard);
            }
        }

        // Calculate the goodness of a move to the depth calculated. Stop the tree if it's goodness goes below minEval.
        void EvalMove(int depth, int minEval) {
            ref MovePlan currentMove = ref this;
            while(currentMove.depth < this.depth + depth) {
                foreach(MovePlan childMove in possibleMoves) {
                    currentMove = childMove;
                    System.Console.Out.WriteLine(currentMove.depth);
                }
            }
        }

        public Board board; // the position after this move is made
        public Move move; // the move this struct represents
        public int depth; // the depth into the tree that this MovePlan represents
        public int eval; // the current evaluation of the goodness of this move
        public List<MovePlan> possibleMoves;  // the list of possible moves (each is another branch in the tree)
    }

}