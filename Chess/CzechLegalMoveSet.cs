using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chess
{
    class CzechLegalMoveSet
    {
        private static position_t pawnPos => new position_t(-1, -1);
        public static CzechChessBoard move(CzechChessBoard b, move_t m, Player player)
        {
            // create a copy of the board
            CzechChessBoard b2 = new CzechChessBoard(b);

            // determine if move is enpassant or castling
            //bool enpassant = (b2.Grid[m.from.number][m.from.letter].piece == Piece.PAWN && isEnPassant(b2, m));
            //bool castle = (b2.Grid[m.from.number][m.from.letter].piece == Piece.KING && Math.Abs(m.to.letter - m.from.letter) == 2);

            // update piece list, remove old position from piece list for moving player
            b2.Pieces[player].Remove(m.from);

            // if move kills a piece directly, remove killed piece from killed player piece list
            //if (b2.Grid[m.to.number][m.to.letter].piece != Piece.NONE && b2.Grid[m.from.number][m.from.letter].player != b2.Grid[m.to.number][m.to.letter].player)
            //    b2.Pieces[b2.Grid[m.to.number][m.to.letter].player].Remove(m.to);

            // add the new piece location to piece list
            b2.Pieces[player].Add(m.to);

            if (m.from.Equals(pawnPos))
            {
                // update board grid
                b2.Grid[m.to.number][m.to.letter] = new piece_t(Piece.PAWN,player);
                b2.Grid[m.to.number][m.to.letter].lastPosition = m.from;
            }
            else
            {
                // update board grid
                b2.Grid[m.to.number][m.to.letter] = new piece_t(b2.Grid[m.from.number][m.from.letter]);
                b2.Grid[m.to.number][m.to.letter].lastPosition = m.from;
                b2.Grid[m.from.number][m.from.letter].piece = Piece.NONE;
            }


            //if (enpassant)
            //{
            //    // if kill was through enpassant determine which direction and remove the killed pawn
            //    int step = (b.Grid[m.from.number][m.from.letter].player == Player.WHITE) ? -1 : 1;
            //    b2.Grid[m.to.number + step][m.to.letter].piece = Piece.NONE;
            //}
            //else if (castle)
            //{
            //    // if no kill but enpassant, update the rook position
            //    if (m.to.letter == 6)
            //    {
            //        b2.Grid[m.to.number][5] = new piece_t(b2.Grid[m.to.number][7]);
            //        b2.Grid[m.to.number][7].piece = Piece.NONE;
            //    }
            //    else
            //    {
            //        b2.Grid[m.to.number][3] = new piece_t(b2.Grid[m.to.number][0]);
            //        b2.Grid[m.to.number][0].piece = Piece.NONE;
            //    }
            //}


            //promotion
            //if (b2.Grid[m.to.number][m.to.letter].piece == Piece.PAWN)
            //{
            //    for (int i = 0; i < 8; i++)
            //    {
            //        if (b2.Grid[0][i].piece == Piece.PAWN)
            //            b2.Grid[0][i].piece = Piece.QUEEN;
            //        if (b2.Grid[7][i].piece == Piece.PAWN)
            //            b2.Grid[7][i].piece = Piece.QUEEN;
            //    }
            //}

            // update king position
            //if (b2.Grid[m.to.number][m.to.letter].piece == Piece.KING)
            //{
            //    b2.Kings[b2.Grid[m.to.number][m.to.letter].player] = m.to;
            //}

            // update last move 
            b2.LastMove[player] = m.to;

            return b2;
        }

        /// <summary>
        /// Determine if the provided player has any valid moves.
        /// </summary>
        /// <param name="b">The state of the game.</param>
        /// <param name="player">The player.</param>
        /// <returns>True if the player has moves.</returns>
        public static bool hasMoves(CzechChessBoard b, Player player)
        {
            foreach (position_t pos in b.Pieces[player])
                if (b.Grid[pos.number][pos.letter].piece != Piece.NONE &&
                    b.Grid[pos.number][pos.letter].player == player &&
                    getLegalMove(b, pos).Count > 0) return true;
            return false;
        }


        /// <summary>
        /// Get all legal moves for the player on the current board.
        /// </summary>
        /// <param name="b">The state of the game.</param>
        /// <param name="player">The player whose moves you want.</param>
        /// <returns>A 1-to-many dictionary of moves from one position to many</returns>
        public static Dictionary<position_t, List<position_t>> getPlayerMoves(CzechChessBoard b, Player player)
        {
            Dictionary<position_t, List<position_t>> moves = new Dictionary<position_t, List<position_t>>();
            foreach (position_t pos in b.Pieces[player])
                if (b.Grid[pos.number][pos.letter].piece != Piece.NONE)
                {
                    if (!moves.ContainsKey(pos))
                        moves[pos] = new List<position_t>();

                    moves[pos].AddRange(CzechLegalMoveSet.getLegalMove(b, pos));
                }

            if (!moves.ContainsKey(pawnPos))
                moves[pawnPos] = new List<position_t>();


            moves[pawnPos].AddRange(getLegalPawnInsert(b, player));
            return moves;
        }

        /// <summary>
        /// Get any legal move from the current position on the provided board.
        /// </summary>
        /// <param name="board">The state of the game.</param>
        /// <param name="pos">The piece/position to check for valid moves.</param>
        /// <param name="verify_check">Whether or not to recurse and check if the current move puts you in check.</param>
        /// <returns>A list of positions the piece can move to.</returns>
        public static List<position_t> getLegalMove(CzechChessBoard board, position_t pos, bool verify_check = true)
        {
            piece_t p = board.Grid[pos.number][pos.letter];
            if (p.piece == Piece.NONE) return new List<position_t>();

            switch (p.piece)
            {
                case Piece.ROOK:
                    return CzechLegalMoveSet.Rook(board, pos, verify_check);
                case Piece.KNIGHT:
                    return CzechLegalMoveSet.Knight(board, pos, verify_check);
                case Piece.BISHOP:
                    return CzechLegalMoveSet.Bishop(board, pos, verify_check);
                default:
                    return new List<position_t>();
            }
        }

        public static List<position_t> getLegalPawnInsert(CzechChessBoard board, Player player)
        {
            var moves = new List<position_t>();
            foreach (position_t pos in board.Pieces[player])
            {
                moves.AddRange(CzechLegalMoveSet.getLegalMove(board, pos));
            }

            // Get possition with 3 or more atack from player
            return moves.GroupBy(p => new { p.number, p.letter }).Where(p => p.Count() > 2).Select(p => new position_t() { number = p.Key.number, letter = p.Key.letter }).ToList();
        }

        /// <summary>
        /// Slide along the path steps until you hit something. Return path to point and if it ends attacking with the attack.
        /// </summary>
        private static List<position_t> Slide(CzechChessBoard board, Player p, position_t pos, position_t step)
        {
            List<position_t> moves = new List<position_t>();
            for (int i = 1; i < 8; i++)
            {
                position_t moved = new position_t(pos.letter + i * step.letter, pos.number + i * step.number);

                if (moved.letter < 0 || moved.letter > 7 || moved.number < 0 || moved.number > 7)
                    break;

                if (board.Grid[moved.number][moved.letter].piece != Piece.NONE)
                {
                    if (board.Grid[moved.number][moved.letter].player != p)
                        moves.Add(moved);
                    break;
                }
                moves.Add(moved);
            }
            return moves;
        }

        public static bool isAttack(CzechChessBoard b, Player player, position_t move)
        {
            CzechChessBoard tempBoard = new CzechChessBoard(b);
            if (tempBoard.Grid[move.number][move.letter].player != player && tempBoard.Grid[move.number][move.letter].piece != Piece.NONE)
                return true;

            return false;
        }
        private static List<position_t> Bishop(CzechChessBoard board, position_t pos, bool verify_check = true)
        {
            List<position_t> moves = new List<position_t>();

            piece_t p = board.Grid[pos.number][pos.letter];
            if (p.piece == Piece.NONE) return moves;

            // slide along diagonals to find available moves
            moves.AddRange(Slide(board, p.player, pos, new position_t(1, 1)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(-1, -1)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(-1, 1)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(1, -1)));

            if (verify_check) // make sure each move doesn't put us in check
            {
                for (int i = moves.Count - 1; i >= 0; i--)
                {
                    if (isAttack(board, p.player, moves[i]))
                    {
                        moves.RemoveAt(i);
                    }
                }
            }
            return moves;
        }

        private static List<position_t> Knight(CzechChessBoard board, position_t pos, bool verify_check = true)
        {
            List<position_t> moves = new List<position_t>();

            piece_t p = board.Grid[pos.number][pos.letter];
            if (p.piece == Piece.NONE) return moves;

            // collect all relative moves possible
            List<position_t> relative = new List<position_t>();

            relative.Add(new position_t(2, 1));
            relative.Add(new position_t(2, -1));

            relative.Add(new position_t(-2, 1));
            relative.Add(new position_t(-2, -1));

            relative.Add(new position_t(1, 2));
            relative.Add(new position_t(-1, 2));

            relative.Add(new position_t(1, -2));
            relative.Add(new position_t(-1, -2));

            // iterate moves
            foreach (position_t move in relative)
            {
                position_t moved = new position_t(move.letter + pos.letter, move.number + pos.number);

                // bounds check
                if (moved.letter < 0 || moved.letter > 7 || moved.number < 0 || moved.number > 7)
                    continue;

                // if empty space or attacking
                if (board.Grid[moved.number][moved.letter].piece == Piece.NONE ||
                    board.Grid[moved.number][moved.letter].player != p.player)
                    moves.Add(moved);
            }

            if (verify_check)// make sure each move doesn't put us in check
            {
                for (int i = moves.Count - 1; i >= 0; i--)
                {
                    if (isAttack(board, p.player, moves[i]))
                    {
                        moves.RemoveAt(i);
                    }
                }
            }
            return moves;
        }

        private static List<position_t> Rook(CzechChessBoard board, position_t pos, bool verify_check = true)
        {
            List<position_t> moves = new List<position_t>();

            piece_t p = board.Grid[pos.number][pos.letter];
            if (p.piece == Piece.NONE) return moves;

            // slide along vert/hor for possible moves
            moves.AddRange(Slide(board, p.player, pos, new position_t(1, 0)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(-1, 0)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(0, 1)));
            moves.AddRange(Slide(board, p.player, pos, new position_t(0, -1)));

            if (verify_check)// make sure each move doesn't put us in check
            {
                for (int i = moves.Count - 1; i >= 0; i--)
                {
                    if (isAttack(board, p.player, moves[i]))
                    {
                        moves.RemoveAt(i);
                    }
                }
            }
            return moves;
        }

        private static List<position_t> Pawn(CzechChessBoard board, position_t pos, bool verify_check = true)
        {
            List<position_t> moves = new List<position_t>();

            piece_t p = board.Grid[pos.number][pos.letter];
            if (p.piece == Piece.NONE) return moves;


            return moves;
        }
    }
}
