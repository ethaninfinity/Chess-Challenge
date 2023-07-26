using ChessChallenge.API;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return board.GetLegalMoves()[0];
    }
}