using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Chess
{
    class CzechChess
    {
        public CzechChessBoard Board { get; private set; }
        public Player Turn { get; private set; }
        public position_t Selection { get; private set; }

        private UIBoard m_UI;
        private int m_nPlayers;

        public CzechChess(UIBoard ui, int nPlayers = 1, bool setupBoard = true)
        {
            // callback setup
            this.m_UI = ui;
            this.m_UI.SetStatus(true, "Generating...");


            // number of players = {0, 1, 2}
            this.m_nPlayers = nPlayers;
            // white always starts
            this.Turn = Player.WHITE;

            // create a new blank board unless setup is true
            this.Board = new CzechChessBoard();
            if (setupBoard)
            {
                this.Board.SetInitialPlacement();
            }

            // update ui
            this.m_UI.SetBoardCzech(Board);
            this.m_UI.SetStatus(false, "White's turn.");
        }


        public bool detectCheckmate()
        {
            //var jooj = this.Board.Grid.Where(p => p.Where(o => o.piece == Piece.PAWN && o.player == Player.WHITE).Any()).Count();
            //var joodsdj = this.Board.Grid.Where(p => p.Where(o => o.piece == Piece.PAWN && o.player == Player.BLACK).Any()).Count();
            
            var blackCount = 0;
            var whiteCount = 0;
            foreach (var list in this.Board.Grid.Where(p => p.Any(o => o.piece == Piece.PAWN && o.player == Player.WHITE)))
            {
                whiteCount += list.Where(p => p.piece == Piece.PAWN && p.player == Player.WHITE).Count();
            }
            foreach (var list in this.Board.Grid.Where(p => p.Any(o => o.piece == Piece.PAWN && o.player == Player.BLACK)))
            {
                blackCount += list.Where(p => p.piece == Piece.PAWN && p.player == Player.BLACK).Count();
            }
            
            bool whiteHavePawns = whiteCount > 7;
            bool blackHavePawns = blackCount > 7;

            if (whiteHavePawns)
            {
                this.m_UI.LogMove("Checkmate!\n");
                this.m_UI.SetStatus(false, "White wins!");
                return true;
            }
            else if (blackHavePawns)
            {
                this.m_UI.LogMove("Checkmate!\n");
                this.m_UI.SetStatus(false, "Black wins!");
                return true;
            }

            return false;
        }

        public void AISelect()
        {
            //wait for previous ai thread to stop
            while (CzechAI.RUNNING)
            {
                Thread.Sleep(100);
            }

            // ai is dump
            this.m_UI.SetStatus(true, "Thinking...");

            // calculate move
            move_t move = CzechAI.MiniMaxAB(this.Board, this.Turn);

            // if valid move, make the move
            if (move.to.letter >= 0 && move.to.number >= 0)
            {
                MakeMove(move);
            }
            else // if invalid move 
            {
                if (!CzechAI.STOP) // and not caused by AI interupt
                {
                    // fuuuuuu
                    this.m_UI.LogMove("Null Move\n");
                }
            }

            bool checkmate = false;

            // if the AI wasn't interupted finish our turn
            if (!CzechAI.STOP)
            {
                switchPlayer();
                checkmate = detectCheckmate();
            }

            // we're done now
            CzechAI.RUNNING = false;

            // if the AI wan't interupted 
            // and we're in AI vs AI mode
            // and not in checkmate/stalemate
            // start the next AI's turn
            if (!CzechAI.STOP && this.m_nPlayers == 0 && !checkmate)
            {
                new Thread(AISelect).Start();
            }
        }

        public virtual List<position_t> Select(position_t pos)
        {
            // has previously selected something
            if (this.Board.Grid[this.Selection.number][this.Selection.letter].piece != Piece.NONE
                && this.Turn == this.Board.Grid[this.Selection.number][this.Selection.letter].player
                && (this.m_nPlayers == 2
                || this.Turn == Player.WHITE))
            {
                // get previous selections moves and determine if we chose a legal one by clicking
                List<position_t> moves = CzechLegalMoveSet.getLegalMove(this.Board, this.Selection);
                moves.AddRange(CzechLegalMoveSet.getLegalPawnInsert(this.Board, this.Turn));
                foreach (position_t move in moves)
                {
                    if (move.Equals(pos))
                    {
                        // we selected a legal move so update the board
                        MakeMove(new move_t(this.Selection, pos));

                        // finish move
                        switchPlayer();
                        if (detectCheckmate()) return new List<position_t>();

                        if (this.m_nPlayers == 1) // start ai
                        {
                            new Thread(AISelect).Start(); // thread turn
                        }
                        return new List<position_t>();
                    }
                }
            }

            // first click, let's show possible moves
            if (this.Board.Grid[pos.number][pos.letter].player == this.Turn && (this.m_nPlayers == 2 || this.Turn == Player.WHITE))
            {
                List<position_t> moves = CzechLegalMoveSet.getLegalMove(this.Board, pos);
                this.Selection = pos;
                return moves;
            }

            // reset
            this.Selection = new position_t();
            return new List<position_t>();
        }

        public virtual bool InsertPawn(position_t pos)
        {
            this.Selection = new position_t();
            // get previous selections moves and determine if we chose a legal one by clicking
            List<position_t> moves = CzechLegalMoveSet.getLegalPawnInsert(this.Board, this.Turn);
            foreach (position_t move in moves)
            {
                if (move.Equals(pos))
                {
                    // we selected a legal move so update the board
                    MakeMove(new move_t(new position_t(-1,-1), pos));

                    // finish move
                    switchPlayer();
                    if (detectCheckmate()) return false;

                    if (this.m_nPlayers == 1) // start ai
                    {
                        new Thread(AISelect).Start(); // thread turn
                    }
                    return true;
                }
            }

            // reset
            this.Selection = new position_t();
            return false;
        }

        private void MakeMove(move_t m)
        {
            // start move log output
            string move = (this.Turn == Player.WHITE) ? "W" : "B";

            move += ":\t";
            if (m.from.Equals(new position_t(-1, -1)))
            {
                move += "P ";
            }
            else
            {
                // piece
                switch (this.Board.Grid[m.from.number][m.from.letter].piece)
                {
                    case Piece.PAWN:
                        move += "P ";
                        break;
                    case Piece.ROOK:
                        move += "R ";
                        break;
                    case Piece.KNIGHT:
                        move += "K ";
                        break;
                    case Piece.BISHOP:
                        move += "B ";
                        break;
                }
            }
            // letter
            switch (m.to.letter)
            {
                case 0: move += "a"; break;
                case 1: move += "b"; break;
                case 2: move += "c"; break;
                case 3: move += "d"; break;
                case 4: move += "e"; break;
                case 5: move += "f"; break;
                case 6: move += "g"; break;
                case 7: move += "h"; break;
            }

            // number
            move += (m.to.number + 1).ToString();

            // update board / make actual move
            this.Board = CzechLegalMoveSet.move(this.Board, m, this.Turn);
            var blackCount = 0;
            var whiteCount = 0;
            foreach(var list in this.Board.Grid.Where(p => p.Any(o => o.piece == Piece.PAWN && o.player == Player.WHITE)))
            {
                whiteCount += list.Where(p => p.piece == Piece.PAWN && p.player == Player.WHITE).Count();
            }
            foreach (var list in this.Board.Grid.Where(p => p.Any(o => o.piece == Piece.PAWN && o.player == Player.BLACK)))
            {
                blackCount += list.Where(p => p.piece == Piece.PAWN && p.player == Player.BLACK).Count();
            }
            this.m_UI.SetPawnCount(whiteCount, blackCount);
            // show log
            this.m_UI.LogMove(move + Environment.NewLine);
        }
        private void switchPlayer()
        {
            this.Turn = (this.Turn == Player.WHITE) ? Player.BLACK : Player.WHITE;
            this.m_UI.SetTurnCzech(this.Turn);
            this.m_UI.SetStatus(false, ((this.Turn == Player.WHITE) ? "White" : "Black") + "'s Turn.");
            this.m_UI.SetBoardCzech(this.Board);
        }
    }
}
