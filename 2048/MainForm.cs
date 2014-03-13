using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// they might be the same thing, but it's for readability
using Vector = System.Tuple<int, int>;
using Position = System.Tuple<int, int>;
using System.Diagnostics;

namespace _2048
{
    public partial class MainForm : Form
    {
        // Variables
        public int[,] field;
        public int score = 0;
        public Random r = new Random();
        // Constants
        const string SCORE_LABEL_PREFIX = "Score: ";
        const int START_TILES = 2;
        static int[] SPAWNABLE = { 2, 2, 2, 4 }; // more of the number means it will be picked more - also not technically a const
        static Vector UP = new Vector(-1, 0);
        static Vector DOWN = new Vector(1, 0);
        static Vector LEFT = new Vector(0, -1);
        static Vector RIGHT = new Vector(0, 1);

        public MainForm()
        {
            InitializeComponent();
            NewGame();
        }

        // Functions
        public static Tuple<int, int> AddTuple(Tuple<int, int> t1, Tuple<int, int> t2)
        {
            return new Tuple<int, int>(t1.Item1 + t2.Item1, t1.Item2 + t2.Item2);
        }

        public static Tuple<int, int> SubtractTuple(Tuple<int, int> t1, Tuple<int, int> t2)
        {
            return new Tuple<int, int>(t1.Item1 - t2.Item1, t1.Item2 - t2.Item2);
        }

        private void UpdateLabels()
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    table.GetControlFromPosition(j, i).Text = field[i, j].ToString();
            statusBar.Text = SCORE_LABEL_PREFIX + score.ToString();
        }

        private void NewGame()
        {
            field = new int[4, 4];
            score = 0;
            for (int i = 0; i < START_TILES; i++)
                SpawnTile();
            UpdateLabels();
        }

        /// <summary>
        /// Spawns an empty tile on the board if it can.
        /// </summary>
        private void SpawnTile()
        {
            if (IsFull()) // if full, don't bother
                return;
            int x = r.Next(4);
            int y = r.Next(4);
            if (IsEmpty(x, y))
            {
                field[x, y] = SPAWNABLE[r.Next(SPAWNABLE.Length)];
                return;
            }
            SpawnTile(); // recurse and try again
        }

        /// <summary>
        /// Checks if the board is full.
        /// </summary>
        /// <returns></returns>
        private bool IsFull()
        {
            foreach (int i in field)
                if (i == 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Check if a tile exists at that position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool IsEmpty(int x, int y)
        {
            try
            {
                return field[x, y] == 0;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a tile can be moved in a Vector.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move in.</param>
        /// <returns></returns>
        private bool CanMove(int x, int y, Vector d)
        {
            return IsEmpty(x + d.Item1, y + d.Item2);
        }

        /// <summary>
        /// Gets how far the tile can move.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move by.</param>
        /// <param name="cd">The Vector to keep adding.</param>
        /// <returns>The vector that you can move with.</returns>
        private Vector HowFarCanMove(int x, int y, Vector d, Vector cd)
        {
            if (CanMove(x, y, d))
                return HowFarCanMove(x, y, AddTuple(d, cd), cd);
            return SubtractTuple(d, cd);
        }

        /// <summary>
        /// Gets how far the tile can move.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move by.</param>
        /// <returns>The vector that you can move with.</returns>
        private Vector HowFarCanMove(int x, int y, Vector d)
        {
            if (CanMove(x, y, d))
                return HowFarCanMove(x, y, AddTuple(d, d), d);
            return SubtractTuple(d, d);
        }

        /// <summary>
        /// Moves a tile to a Vector, and leaves behind it nothing.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move in.</param>
        /// <returns>The new position.</returns>
        private Position MoveTile(int x, int y, Vector d)
        {
            field[x + d.Item1, y + d.Item2] = field[x, y];
            field[x, y] = 0;
            return new Position(x + d.Item1, y + d.Item2);
        }

        /// <summary>
        /// Check if a tile can be merged in a Vector.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move in.</param>
        /// <returns></returns>
        private bool CanMerge(int x, int y, Vector d)
        {
            try
            {
                return field[x + d.Item1, y + d.Item2] == field[x, y];
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Merges two tiles.
        /// </summary>
        /// <param name="x">The source tile.</param>
        /// <param name="y">The source tile.</param>
        /// <param name="d">The Vector to move in.</param>
        /// <returns>The new position of the merged tiles.</returns>
        /// <remarks>This does NOT check if the tiles should be merged - you should run CanMerge first.</remarks>
        private Position Merge(int x, int y, Vector d)
        {
            int s1 = field[x, y];
            int s2 = field[x + d.Item1, y + d.Item2];
            Position p = MoveTile(x, y, d);
            field[p.Item1, p.Item2] = s1 + s2;
            score += field[p.Item1, p.Item2];
            return p;
        }

        private bool AttemptMoveInDirection(int x, int y, Vector d)
        {
            bool didDoSomething = false;
            if (!IsEmpty(x, y))
            {
                if (CanMove(x, y, d))
                {
                    Position p = MoveTile(x, y, HowFarCanMove(x, y, d));
                    Debug.WriteLine(String.Format("Moving {0},{1} to {2},{3}", x, y, p.Item1, p.Item2));
                    if (CanMerge(p.Item1, p.Item2, d))
                        Merge(p.Item1, p.Item2, d);
                    didDoSomething = true;
                }
                // Moving it would have made it 0, right?
                if (CanMerge(x, y, d))
                {
                    Debug.WriteLine(String.Format("Nerging {0},{1} to {2},{3}", x, y, x + d.Item1, y + d.Item2));
                    Merge(x, y, d);
                    didDoSomething = true;
                }
            }
            return didDoSomething;
        }

        private void MoveUp()
        {
            Debug.WriteLine("Moving up!");
            Debug.Indent();
            bool didDoSomething = false;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (AttemptMoveInDirection(i, j, UP) & !didDoSomething)
                        didDoSomething = true;
            if (didDoSomething)
                SpawnTile();
            Debug.Unindent();
            UpdateLabels();
        }

        private void MoveDown()
        {
            Debug.WriteLine("Moving left!");
            Debug.Indent();
            bool didDoSomething = false;
            for (int i = 3; i > -1; i--)
                for (int j = 0; j < 4; j++)
                    if (AttemptMoveInDirection(i, j, DOWN) & !didDoSomething)
                        didDoSomething = true;
            if (didDoSomething)
                SpawnTile();
            Debug.Unindent();
            UpdateLabels();
        }

        private void MoveLeft()
        {
            Debug.WriteLine("Moving left!");
            Debug.Indent();
            bool didDoSomething = false;
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 4; i++)
                    if (AttemptMoveInDirection(i, j, LEFT) & !didDoSomething)
                        didDoSomething = true;
            if (didDoSomething)
                SpawnTile();
            Debug.Unindent();
            UpdateLabels();
        }

        private void MoveRight()
        {
            Debug.WriteLine("Moving right!");
            Debug.Indent();
            bool didDoSomething = false;
            for (int j = 3; j > -1; j--)
                for (int i = 0; i < 4; i++)
                    if (AttemptMoveInDirection(i, j, RIGHT) & !didDoSomething)
                        didDoSomething = true;
            if (didDoSomething)
                SpawnTile();
            Debug.Unindent();
            UpdateLabels();
        }

        // Events

        private void newMenu_Click(object sender, EventArgs e)
        {
            NewGame();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    MoveUp();
                    break;
                case Keys.Down:
                    MoveDown();
                    break;
                case Keys.Left:
                    MoveLeft();
                    break;
                case Keys.Right:
                    MoveRight();
                    break;
                default:
                    break;
            }
        }

        private void quitMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
