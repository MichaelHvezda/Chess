using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Chess
{
    public partial class MainForm : Form, UIBoard
    {
        bool m_aigame = false;
        bool m_checkmate = false;
        bool m_manualBoard = false; // Don't init board on new game
        bool m_finalizedBoard = false;
        Player m_manualPlayer = Player.WHITE;
        Piece m_manualPiece = Piece.PAWN;

        Chess chess;
        CzechChess czechChess;
                
        /// <summary>
        /// Stop all current activity / games and reset everything.
        /// </summary>
        private void Stop()
        {
            SetStatus(false, "Choose New Game or Manual Board.");

            // stop the ai and reset chess
            AI.STOP = true;
            chess = null;

            // reset turn indicator
            SetTurn(Player.WHITE);
            InsertPawn.Visible = false;
            // reset timers
            tmrWhite.Stop();
            tmrBlack.Stop();
            m_whiteTime = new TimeSpan(0);
            m_blackTime = new TimeSpan(0);
            lblWhiteTime.Text = m_whiteTime.ToString("c");
            lblBlackTime.Text = m_blackTime.ToString("c");

            // clear the board ui and log
            SetBoard(new ChessBoard());
            txtLog.Text = "";

            // reset board status vars
            m_checkmate = false;
            m_aigame = false;
            m_finalizedBoard = false;

            // reset the menu
            manualBoardToolStripMenuItem.Enabled = true;
            endCurrentGameToolStripMenuItem.Enabled = false;
            if (m_finalizedBoard || m_manualBoard)
            {
                setPieceToolStripMenuItem.Enabled = false;
                manualBoardToolStripMenuItem.Checked = false;
            }
            endCurrentGameToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// Set up a new game for the specified number of players.
        /// </summary>
        private void NewGame(int nPlayers)
        {
            // clean up all of the things first
            if (!m_manualBoard) Stop();

            // create new game for number of players
            m_aigame = (nPlayers == 0);
            chess = new Chess(this, nPlayers, !m_manualBoard);

            // show turn status
            SetTurn(Player.WHITE);
            SetStatus(false, "White's turn");

            // reset timers
            m_whiteTime = new TimeSpan(0);
            m_blackTime = new TimeSpan(0);
            lblWhiteTime.Text = m_whiteTime.ToString("c");
            lblBlackTime.Text = m_blackTime.ToString("c");

            // show ai difficulty
            if (nPlayers < 2)
            {
                LogMove("AI Difficulty " + (string)temp.Tag + "\n");
            }

            if (m_manualBoard)
            {
                // allow setting up the board
                SetStatus(false, "You may now place pieces via the menu.");
                setPieceToolStripMenuItem.Enabled = true;
            }
            else
            {
                // start the game
                SetStatus(false, "White's Turn");
                if (m_aigame)
                {
                    new Thread(chess.AISelect).Start();
                }
                tmrWhite.Start();
            }

            // allow stopping the game
            endCurrentGameToolStripMenuItem.Enabled = true;
        }

        private void NewGameCzech(int nPlayers)
        {
            // clean up all of the things first
            if (!m_manualBoard) Stop();

            // create new game for number of players
            m_aigame = (nPlayers == 0);
            czechChess = new CzechChess(this, nPlayers, !m_manualBoard);

            InsertPawn.Visible = true;
            // show turn status
            SetTurnCzech(Player.WHITE);
            SetStatus(false, "White's turn");

            // reset timers
            m_whiteTime = new TimeSpan(0);
            m_blackTime = new TimeSpan(0);
            lblWhiteTime.Text = m_whiteTime.ToString("c");
            lblBlackTime.Text = m_blackTime.ToString("c");

            // show ai difficulty
            if (nPlayers < 2)
            {
                LogMove("AI Difficulty " + (string)temp.Tag + "\n");
            }

            if (m_manualBoard)
            {
                // allow setting up the board
                SetStatus(false, "You may now place pieces via the menu.");
                setPieceToolStripMenuItem.Enabled = true;
            }
            else
            {
                // start the game
                SetStatus(false, "White's Turn");
                if (m_aigame)
                {
                    new Thread(czechChess.AISelect).Start();
                }
                tmrWhite.Start();
            }

            // allow stopping the game
            endCurrentGameToolStripMenuItem.Enabled = true;
        }

        private void InsertPawns()
        {
            var player = new Player();
            if (tmrWhite.Enabled)
            {
                player = Player.WHITE;
            } else if (tmrBlack.Enabled)
            {
                player = Player.BLACK;
            }
            // clear board
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    Board[i][j].BackColor = ((i + j) % 2 == 0) ? Color.Black : Color.White;
            List<position_t> moves = CzechLegalMoveSet.getLegalPawnInsert(czechChess.Board, player);
            foreach (position_t move in moves)
            {
                Board[move.number][move.letter].BackColor = Color.Yellow;
            }
            m_manualPlayer = player;
            m_manualBoard = true;
            //foreach (position_t move in moves)
            //{
            //    if ((czechChess.Board.Grid[move.number][move.letter].player != czechChess.Turn
            //        && czechChess.Board.Grid[move.number][move.letter].piece != Piece.NONE))
            //    {
            //        // attack
            //        Board[move.number][move.letter].BackColor = Color.Red;
            //    }
            //    else
            //    {
            //        // move
            //        Board[move.number][move.letter].BackColor = Color.Yellow;
            //    }
            //}
        }

        public void SetTurn(Player p)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetTurn(p)));
                return;
            }

            // update the turn indicator
            if (chess != null)
            {
                picTurn.Image = graphics.TurnIndicator[chess.Turn];
            }
            else
            {
                picTurn.Image = graphics.TurnIndicator[Player.WHITE];
            }

            // if not creating a board
            if (!m_manualBoard)
            {
                // toggle whos timer is running
                if (p == Player.WHITE)
                {
                    tmrBlack.Stop();
                    tmrWhite.Start();
                }
                else
                {
                    tmrWhite.Stop();
                    tmrBlack.Start();
                }

                // if game over just stop timers
                if (chess != null && (m_checkmate || chess.detectCheckmate()))
                {
                    tmrWhite.Stop();
                    tmrBlack.Stop();
                }
            }
        }

        public void SetTurnCzech(Player p)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetTurnCzech(p)));
                return;
            }

            // update the turn indicator
            if (czechChess != null)
            {
                picTurn.Image = graphics.TurnIndicator[czechChess.Turn];
            }
            else
            {
                picTurn.Image = graphics.TurnIndicator[Player.WHITE];
            }

            // if not creating a board
            if (!m_manualBoard)
            {
                // toggle whos timer is running
                if (p == Player.WHITE)
                {
                    tmrBlack.Stop();
                    tmrWhite.Start();
                }
                else
                {
                    tmrWhite.Stop();
                    tmrBlack.Start();
                }

                // if game over just stop timers
                if (czechChess != null && (m_checkmate || czechChess.detectCheckmate()))
                {
                    tmrWhite.Stop();
                    tmrBlack.Stop();
                }
            }
        }

        public void SetBoard(ChessBoard board)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetBoard(board)));
                return;
            }

            // update all tiles on board
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    SetPiece(board.Grid[i][j].piece, board.Grid[i][j].player, j, i);
        }

        public void SetBoardCzech(CzechChessBoard board)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetBoardCzech(board)));
                return;
            }

            // update all tiles on board
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    SetPiece(board.Grid[i][j].piece, board.Grid[i][j].player, j, i);
        }

        public void LogMove(string move)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMove(move)));
                return;
            }

            // reset check indicators
            lblWhiteCheck.Visible = false;
            lblBlackCheck.Visible = false;

            // show check indicator
            if (move.Contains("+"))
            {
                lblWhiteCheck.Visible = chess.Turn == Player.BLACK;
                lblBlackCheck.Visible = chess.Turn == Player.WHITE;
            }

            txtLog.AppendText(move);
        }

        public void SetStatus(bool thinking, string message)
        {
            // if a thread called this, invoke recursion
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetStatus(thinking, message)));
                return;
            }

            // update status text and progress bar
            lblStatus.Text = message;
            if (thinking)
            {
                prgThinking.MarqueeAnimationSpeed = 30;
                prgThinking.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                prgThinking.MarqueeAnimationSpeed = 0;
                prgThinking.Value = 0;
                prgThinking.Style = ProgressBarStyle.Continuous;
            }
        }

        public void SetPawnCount(int white,int black)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetPawnCount(white,black)));
                return;
            }
            whiteText1.Text = "White count " + white;
            blackText1.Text = "Black count " + black;
        }
    }
}
