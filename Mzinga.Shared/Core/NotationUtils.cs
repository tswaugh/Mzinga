﻿// 
// NotationUtils.cs
//  
// Author:
//       Jon Thysell <thysell@gmail.com>
// 
// Copyright (c) 2018 Jon Thysell <http://jonthysell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;

namespace Mzinga.Core
{
    public class NotationUtils
    {
        public static Move ParseMoveString(Board board, string moveString)
        {
            if (null == board)
            {
                throw new ArgumentNullException("board");
            }

            if (string.IsNullOrWhiteSpace(moveString))
            {
                throw new ArgumentNullException("moveString");
            }

            moveString = moveString.Trim();

            try
            {
                // Attempt to parse as an algebraic move
                return new Move(moveString);
            }
            catch (Exception) { }

            string[] moveStringParts = moveString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            PieceName movingPiece = EnumUtils.ParseShortName(moveStringParts[0]);

            if (moveStringParts.Length == 1)
            {
                if (board.BoardState == BoardState.NotStarted)
                {
                    // First move is on the origin
                    return new Move(movingPiece, Position.Origin);
                }

                throw new ArgumentException("You must specify a target piece.", "moveString");
            }

            string targetString = moveStringParts[1].Trim('-', '/', '\\');
            int seperatorIndex = moveStringParts[1].IndexOfAny(new char[] { '-', '/', '\\' });

            PieceName targetPiece = EnumUtils.ParseShortName(targetString);

            if (seperatorIndex < 0)
            {
                // Putting a piece on top of another
                return new Move(movingPiece, board.GetPiecePosition(targetPiece).GetAbove());
            }

            char seperator = moveStringParts[1][seperatorIndex];

            Position targetPosition = board.GetPiecePosition(targetPiece);

            if (seperatorIndex == 0)
            {
                // Moving piece on the left-hand side of the target piece
                switch (seperator)
                {
                    case '-':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.UpLeft));
                    case '/':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.DownLeft));
                    case '\\':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.Up));
                }
            }
            else if (seperatorIndex == targetString.Length)
            {
                // Moving piece on the right-hand side of the target piece
                switch (seperator)
                {
                    case '-':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.DownRight));
                    case '/':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.UpRight));
                    case '\\':
                        return new Move(movingPiece, targetPosition.NeighborAt(Direction.Down));
                }
            }

            return null;
        }

        public static string ToBoardSpaceMoveString(Board board, Move move)
        {
            if (null == board)
            {
                throw new ArgumentNullException("board");
            }

            if (null == move)
            {
                throw new ArgumentNullException("move");
            }

            if (move.IsPass)
            {
                return BoardSpacePass;
            }


            if (move.Color != board.CurrentTurnColor)
            {
                return null;
            }

            string startPiece = ToBoardSpacePieceName(move.PieceName);

            if (board.CurrentTurn == 0)
            {
                return startPiece;
            }
            else
            {
                string endPiece = "";

                if (move.Position.Stack > 0)
                {
                    // On top of board
                    PieceName pieceBelow = board.GetPiece(move.Position.GetBelow());
                    endPiece = ToBoardSpacePieceName(pieceBelow);
                }
                else
                {
                    // Find neighbor to move.Position
                    foreach (Direction dir in EnumUtils.Directions)
                    {
                        // Found a neighbor!

                        Position pos = move.Position.NeighborAt(dir);
                        PieceName neighbor = board.GetPieceOnTop(pos);

                        if (neighbor == move.PieceName)
                        {
                            Position posBelow = board.GetPiecePosition(neighbor).GetBelow();
                            neighbor = null != posBelow ? board.GetPiece(posBelow) : PieceName.INVALID;
                        }

                        if (neighbor != PieceName.INVALID)
                        {
                            endPiece = ToBoardSpacePieceName(neighbor);

                            switch (dir)
                            {
                                case Direction.Up:
                                    endPiece = endPiece + @"\";
                                    break;
                                case Direction.UpRight:
                                    endPiece = @"/" + endPiece;
                                    break;
                                case Direction.DownRight:
                                    endPiece = @"-" + endPiece;
                                    break;
                                case Direction.Down:
                                    endPiece = @"\" + endPiece;
                                    break;
                                case Direction.DownLeft:
                                    endPiece = endPiece + @"/";
                                    break;
                                case Direction.UpLeft:
                                    endPiece = endPiece + @"-";
                                    break;
                            }

                            break;
                        }
                    }
                }

                return string.IsNullOrWhiteSpace(endPiece) ? startPiece : string.Format("{0} {1}", startPiece, endPiece);
            }
        }

        public static string NormalizeBoardSpaceMoveString(string moveString)
        {
            if (string.IsNullOrWhiteSpace(moveString))
            {
                throw new ArgumentNullException("moveString");
            }

            string[] moveStringParts = moveString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            PieceName movingPiece = EnumUtils.ParseShortName(moveStringParts[0]);

            if (moveStringParts.Length == 1)
            {
                return ToBoardSpacePieceName(movingPiece);
            }

            string targetString = moveStringParts[1].Trim('-', '/', '\\');
            int seperatorIndex = moveStringParts[1].IndexOfAny(new char[] { '-', '/', '\\' });

            PieceName targetPiece = EnumUtils.ParseShortName(targetString);

            if (seperatorIndex < 0)
            {
                return string.Format("{0} {1}", ToBoardSpacePieceName(movingPiece), ToBoardSpacePieceName(targetPiece));
            }

            char seperator = moveStringParts[1][seperatorIndex];
            if (seperatorIndex == 0)
            {
                // Moving piece on the left-hand side of the target piece
                return string.Format("{0} {1}{2}", ToBoardSpacePieceName(movingPiece), seperator, ToBoardSpacePieceName(targetPiece));
            }
            else if (seperatorIndex == targetString.Length)
            {
                // Moving piece on the right-hand side of the target piece
                return string.Format("{0} {1}{2}", ToBoardSpacePieceName(movingPiece), ToBoardSpacePieceName(targetPiece), seperator);
            }

            return null;
        }

        public static MoveSet ParseMoveStringList(Board board, string moveStringList)
        {
            if (null == board)
            {
                throw new ArgumentNullException("board");
            }

            if (string.IsNullOrWhiteSpace(moveStringList))
            {
                throw new ArgumentNullException("moveStringList");
            }

            string[] split = moveStringList.Split(new char[] { MoveSet.MoveStringSeparator }, StringSplitOptions.RemoveEmptyEntries);

            MoveSet moves = new MoveSet();
            for (int i = 0; i < split.Length; i++)
            {
                Move parseMove = ParseMoveString(board, split[i]);
                moves.Add(parseMove);
            }
            return moves;
        }

        public static string ToBoardSpaceMoveStringList(Board board, MoveSet moves)
        {
            if (null == board)
            {
                throw new ArgumentNullException("board");
            }

            if (null == moves)
            {
                throw new ArgumentNullException("moves");
            }

            StringBuilder sb = new StringBuilder();

            foreach (Move move in moves)
            {
                sb.AppendFormat("{0}{1}", ToBoardSpaceMoveString(board, move), MoveSet.MoveStringSeparator);
            }

            return sb.ToString().TrimEnd(MoveSet.MoveStringSeparator);
        }

        public static string ToBoardSpacePieceName(PieceName pieceName)
        {
            string name = EnumUtils.GetShortName(pieceName);

            if (null != name && name.Length > 0)
            {
                name = name[0].ToString().ToLower() + name.Substring(1);
            }

            return name;
        }

        public const string BoardSpacePass = "pass";
    }
}