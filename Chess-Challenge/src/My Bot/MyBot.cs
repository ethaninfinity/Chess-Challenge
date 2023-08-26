using ChessChallenge.API;
using System;

public class MyBot : IChessBot {
    MoveTree _moveTree;
    static bool _isWhite;
    static readonly int Moves = 4;
    static int NumMoves;
    static int ms;

    public MyBot() {
        if (NumMoves > 0)
            Console.WriteLine(ms / NumMoves + " = Average time per turn so far.");
    }

    public Move Think(Board board, Timer timer) {
        _isWhite = board.IsWhiteToMove;
        _moveTree = new(board, Moves);

        _moveTree = _moveTree.ChooseBestMove(board);
        Console.WriteLine("Basic eval: " + BasicEvalPos(board));
        Console.WriteLine("Advanced eval: " + _moveTree.TreeBase.Eval);
        ms += timer.MillisecondsElapsedThisTurn;
        Console.WriteLine(timer.MillisecondsElapsedThisTurn + "ms | " + Moves);
        ++NumMoves;
        return _moveTree.TreeBase.Move;
    }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    // Get the basic piece advantage of the current position
    public static int BasicEvalPos(Board board) {
        int eval = 0;
        foreach(PieceList pieces in board.GetAllPieceLists()) {
            int isMyPieceMultiplier = pieces.IsWhitePieceList == _isWhite ? 1 : -1;
            int pieceValue = PieceValues[(int)pieces.TypeOfPieceInList];
            int scratch = 0;
            switch(pieces.TypeOfPieceInList) {
                case PieceType.Pawn:
                    foreach(Piece piece in pieces)
                        scratch += pieces.IsWhitePieceList ? piece.Square.Rank : 7 - piece.Square.Rank;
                    eval += (pieceValue * pieces.Count + scratch * 10) * isMyPieceMultiplier;
                    break;
                default:
                    eval += pieceValue * pieces.Count * isMyPieceMultiplier;
                    break;
            }
        }
        return board.IsInCheckmate() ? _isWhite != board.IsWhiteToMove ? int.MaxValue : int.MinValue : board.IsDraw() ? eval - 1000 : eval;
    }
}

public class Node {
    public Node[]? Children;  // A list of possible moves that can follow this move.
    public Node? Parent;
    public Move Move;
    public int Depth;
    public int Eval;
    public bool ShouldBeCulled;
    private int _exploredChildIndex;
    private int _childIndex;

    public Node() {
        Eval = Int32.MinValue;
    }

    public Node(Board board, Move move, Node parent) {
        Move = move;
        Parent = parent;
        Depth = parent.Depth + 1;
        board.MakeMove(move);
        Eval = MyBot.BasicEvalPos(board);
        board.UndoMove(move);
    }

//    public Node GetParent(int level) {
//        Node? p = Parent;
//        while (p != null && level-- > 0) p = p.Parent;
//        return p ?? this;
//    }

    public Node? GetNextChild() {
        return Children == null || (_childIndex %= Children.Length + 1) == Children.Length ? null : Children[_childIndex++];
    }

    public Node? GetNextUnexploredChild() {
        return Children != null && _exploredChildIndex < Children.Length ? Children[_exploredChildIndex++]: null;
    }
}

class MoveTree {
    private static readonly Random Rng = new();
    public Node TreeBase;
    private int _maxTreeDepth;

    public void MakeBranch(Board board, Node node) {
        Span<Move> moves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref moves);
        Array.Resize(ref node.Children, moves.Length);
        for (int i = 0; i < moves.Length;)
            node.Children[i] = new(board, moves[i++], node);
    }

    // Create a new tree calculated to maxTreeDepth
    public MoveTree (Board board, int maxTreeDepth) {
        _maxTreeDepth = maxTreeDepth;
        TreeBase = new();
        Node node = TreeBase;
        Node? nextChild;
        do {
            // Generate all children in this node
            if (node.Depth < maxTreeDepth + TreeBase.Depth && !ShouldNodeBeCulled(node))
                MakeBranch(board, node);

            // Find the next unexplored node in the tree
            while ((nextChild = node.GetNextUnexploredChild()) == null && node.Parent != null) {
                board.UndoMove(node.Move);
                node = node.Parent;
            }
            if (nextChild != null) {
                board.MakeMove(nextChild.Move);
                node = nextChild;
            }
        } while (nextChild != null && node != TreeBase);
    }

    public bool ShouldNodeBeCulled(Node node) {
        return false;
//        if (node.ShouldBeCulled) return true;
//        Node? parent;
//        if ((parent = node.GetParent(2)) == null) return false;
//        List<int> evalHistory = new() {node.Eval, parent.Eval};
//        node.ShouldBeCulled = evalHistory.Max() + 20 < TreeBase.Eval;
//        return node.ShouldBeCulled;
    }

    // Choose the best move to make long-term
    public MoveTree ChooseBestMove(Board board) {
        Node node = TreeBase;
        Node? nextChild;
        bool ourTurn = false;
        do {
            // Find the next unexplored node in the tree
            while ((nextChild = node.GetNextChild()) == null && node.Parent != null) {
                // Compare this move's eval to the parent
                if (!node.ShouldBeCulled && node != TreeBase && ourTurn ^ node.Eval < node.Parent.Eval)
                    node.Parent.Eval = node.Eval;
                ourTurn ^= true;
                board.UndoMove(node.Move);
                node = node.Parent;
            }
            if (nextChild != null) {
                ourTurn ^= true;
                board.MakeMove(nextChild.Move);
                node = nextChild;
            }
        } while (nextChild != null && node != TreeBase);
        Node[] bestMoves = Array.FindAll(TreeBase.Children, x => x.Eval >= TreeBase.Eval);
        TreeBase = bestMoves.Length == 0 ?
            TreeBase.Children[0]
        :
            bestMoves[0];
//            bestMoves[Rng.Next(bestMoves.Length)];
        return this;
    }
}