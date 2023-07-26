using ChessChallenge.API;
using System.Collections.Generic;
public class FirstBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    public Move Think(Board board, Timer timer)
    {
        List<Move> moves = new List<Move>();
        moves.AddRange(board.GetLegalMoves());
        
        int eval = basicEval(board);
        if(!board.IsWhiteToMove) {
            eval *= -1;
        }

        foreach(Move move in WarnBlunders(board)) {
            moves.Remove(move);
        }
        
        Move checkMate = findCheckmate(board);
        if(checkMate != Move.NullMove) {
            return checkMate;
        } else if(timer.MillisecondsElapsedThisTurn < 1000) {
            Board testBoard = board;
            foreach(Move testMove in moves) {
                testBoard.MakeMove(testMove);
                Move[] otherMoves = testBoard.GetLegalMoves();
                foreach (Move otherMove in otherMoves) {
                    testBoard.MakeMove(otherMove);
                    Move mateInTwo = findCheckmate(testBoard);
                    if(mateInTwo != Move.NullMove) {
                        testBoard = board;
                        return testMove;
                    }
                    testBoard.UndoMove(otherMove);
                }
                testBoard.UndoMove(testMove);
            }
        }
        List<Move> bestMoves = new List<Move>();
        foreach(Move move in moves) {
            if(move.IsEnPassant) {
                return move;
            } else if(move.IsCapture) {
                if(EvaluateCapture(board, move) > 0) {
                    return move;
                } else if (EvaluateCapture(board, move) == 0) {
                    if(eval > 100) {
                        return move;
                    }
                    bestMoves.Add(move);
                }
            } else if(move.IsPromotion && move.PromotionPieceType == PieceType.Queen && !board.SquareIsAttackedByOpponent(move.TargetSquare)) {
                return move;
            } else if(move.IsCastles) {
                bestMoves.Add(move);
            } else if(move.MovePieceType == PieceType.Pawn) {
                if(EvaluateCapture(board, move) >= 0) {
                    bestMoves.Add(move);
                }
            }
        }
        System.Random rng = new();
        if (bestMoves.Count > 0) {
            return bestMoves[rng.Next(bestMoves.Count)];
        }
        foreach(Move currMove in moves.ToArray()) {
            if(EvaluateCapture(board, currMove) < 0) {
                moves.Remove(currMove);
            }
        }
        if(moves.Count > 0) {
            return moves[rng.Next(moves.Count)];
        }
        return board.GetLegalMoves()[0];
    }

    public Move findCheckmate(Board board) {
        Move[] myMoves = board.GetLegalMoves();
        foreach(Move move in myMoves) {
            if (MoveIsCheckmate(board, move)) {
                return move;
            }
        }
        return Move.NullMove;
    }

    // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        int EvaluateCapture(Board board, Move move) {
            if(board.SquareIsAttackedByOpponent(move.TargetSquare)) {
                    return pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
            }
            return pieceValues[(int)move.CapturePieceType];
        }
        List<Move> WarnBlunders(Board board) {
            List<Move> blunders = new List<Move>();
            foreach(Move move in board.GetLegalMoves()) {
                board.MakeMove(move);
                foreach(Move enemyMove in board.GetLegalMoves()) {
                    if(EvaluateCapture(board, enemyMove) > 0) {
                        blunders.Add(move);
                    } else if (enemyMove.IsPromotion && !board.SquareIsAttackedByOpponent(enemyMove.TargetSquare)) {
                        blunders.Add(move);
                    } else if (MoveIsCheckmate(board, enemyMove)) {
                        blunders.Add(move);
                    }
                }
                board.UndoMove(move);
            }
            return blunders;
        }

        int basicEval(Board board) {
            int eval = 0;
            foreach(Piece piece in board.GetPieceList(PieceType.Pawn, true)) {
                eval += pieceValues[(int)PieceType.Pawn];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Bishop, true)) {
                eval += pieceValues[(int)PieceType.Bishop];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Knight, true)) {
                eval += pieceValues[(int)PieceType.Knight];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Rook, true)) {
                eval += pieceValues[(int)PieceType.Rook];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Queen, true)) {
                eval += pieceValues[(int)PieceType.Queen];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Pawn, false)) {
                eval -= pieceValues[(int)PieceType.Pawn];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Bishop, false)) {
                eval -= pieceValues[(int)PieceType.Bishop];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Knight, false)) {
                eval -= pieceValues[(int)PieceType.Knight];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Rook, false)) {
                eval -= pieceValues[(int)PieceType.Rook];
            }
            foreach(Piece piece in board.GetPieceList(PieceType.Queen, false)) {
                eval -= pieceValues[(int)PieceType.Queen];
            }
            return eval;
        }
}