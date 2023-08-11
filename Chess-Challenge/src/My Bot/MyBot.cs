using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

public class MyBot : IChessBot {
    MoveTree moveTree;
    static bool isWhite;

    public Move Think(Board board, Timer timer) {
        isWhite = board.IsWhiteToMove;
        moveTree = new(board, 4);

        moveTree = moveTree.ChooseBestMove(board);
        Console.WriteLine("Basic eval: " + BasicEvalPos(board));
        Console.WriteLine("Advanced eval: " + moveTree.treeBase.eval);
        return moveTree.GetMove();
    }


    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly static int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    // Get the basic piece advantage of the current position
    public static int BasicEvalPos(Board board) {
        int eval = 0;
        foreach(PieceList pieces in board.GetAllPieceLists()) {
            int isMyPieceMultiplier = (pieces.IsWhitePieceList == isWhite ? 1 : -1);
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
        if(board.IsInCheckmate()) {
            if(isWhite != board.IsWhiteToMove) {
                return int.MaxValue;
            } else {
                return int.MinValue;
            }
        }
        if(board.IsDraw()) eval -= 1000;
        return eval;
    }
}

public class Node {
    public Node?[] children;  // A list of possible moves that can follow this move.
    public Node? parent;
    public Move move;
    public int depth;
    public int eval;
    public bool shouldBeCulled;
    private int exploredChildIndex;
    private int childIndex;

    public Node(Board board) {
        eval = -int.MaxValue;
        depth = 0;
        parent = null;
        move = Move.NullMove;
        children = null;
        exploredChildIndex = 0;
        shouldBeCulled = false;
    }

    public Node(Board board, Move move, Node? parent) {
        this.move = move;
        this.parent = parent;
        depth = parent.depth + 1;
        board.MakeMove(move);
        eval = MyBot.BasicEvalPos(board);
        board.UndoMove(move);
        children = null;
        exploredChildIndex = 0;
        shouldBeCulled = false;
    }

    public Node? GetParent(int level) {
        Node? p = parent;
        while (p != null && level-- > 0) p = p.parent;
        if (p == null) return this;
        return p;
    }

    public Node? GetNextChild() {
        if (children != null && childIndex < children.Length) 
            return children[childIndex++];
        childIndex = 0;
        return null;
    }

    public Node? GetNextUnexploredChild() {
        if(children != null && exploredChildIndex < children.Length)
            return children[exploredChildIndex++];
        return null;
    }
}

class MoveTree {
    private readonly static Random rng = new();
    public Node treeBase;
    private int maxTreeDepth;

    public Move GetMove() {
        return treeBase.move;
    }

    public void MakeBranch(Board board, Node node) {
        System.Span<Move> moves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref moves);
        // Move[] moves = board.GetLegalMoves();
        Array.Resize(ref node.children, moves.Length);
        for (int i = 0; i < moves.Length; ++i)
            node.children[i] = new(board, moves[i], node);
    }

    // Create a new tree calculated to maxTreeDepth
    public MoveTree (Board board, int maxTreeDepth) {
        this.maxTreeDepth = maxTreeDepth;
        treeBase = new(board);
        Node node = treeBase;
        Node? nextChild;
        do {
            // Generate all children in this node
            if (node.depth < maxTreeDepth + treeBase.depth && !ShouldNodeBeCulled(node))
                MakeBranch(board, node);

            // Find the next unexplored node in the tree
            while ((nextChild = node.GetNextUnexploredChild()) == null && node.parent != null) {
                board.UndoMove(node.move);
                node = node.parent;
            }
            if (nextChild != null) {
                board.MakeMove(nextChild.move);
                node = nextChild;
            }
        } while (nextChild != null && node != treeBase);
    }

    public bool ShouldNodeBeCulled(Node node) {
        return false;
        if (node.shouldBeCulled) return true;
        Node? parent;
        if ((parent = node.GetParent(2)) == null) return false;
        List<int> evalHistory = new() {node.eval, parent.eval};
        node.shouldBeCulled = evalHistory.Max() + 20 < treeBase.eval;
        return node.shouldBeCulled;
    }

    // Choose the best move to make long-term
    public MoveTree ChooseBestMove(Board board) {
        Node node = treeBase;
        Node? nextChild;
        bool ourTurn = false;
        do{
            // Find the next unexplored node in the tree
            while ((nextChild = node.GetNextChild()) == null && node.parent != null) {
                // Compare this move's eval to the parent
                if (!ShouldNodeBeCulled(node) && node != treeBase) {
                    if(ourTurn)
                        node.parent.eval = Math.Max(node.parent.eval, node.eval);
                    else
                        node.parent.eval = Math.Min(node.parent.eval, node.eval);
                }
                ourTurn ^= true;
                board.UndoMove(node.move);
                node = node.parent;
            }
            if (nextChild != null) {
                ourTurn ^= true;
                board.MakeMove(nextChild.move);
                node = nextChild;
            }
        } while (nextChild != null && node != treeBase);
        List<Node> bestMoves = new();
        int maxEval = treeBase.eval;
        foreach (Node currentNode in treeBase.children)
            if (currentNode.eval >= maxEval)
                bestMoves.Add(currentNode);
        if (bestMoves.Count == 0)
            treeBase = treeBase.children[0];
        else
            treeBase = bestMoves[rng.Next(bestMoves.Count)];
        return this;
    }
}