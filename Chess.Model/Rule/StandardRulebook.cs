//-----------------------------------------------------------------------
// <copyright file="StandardRulebook.cs">
//     Copyright (c) Michael Szvetits. All rights reserved.
// </copyright>
// <author>Michael Szvetits</author>
//-----------------------------------------------------------------------
namespace Chess.Model.Rule
{
    using Chess.Model.Command;
    using Chess.Model.Data;
    using Chess.Model.Game;
    using Chess.Model.Piece;
    using Chess.Model.Visitor;
	using System;
	using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Represents the standard chess rulebook.
    /// </summary>
    public class StandardRulebook : IRulebook
    {
        /// <summary>
        /// Represents the check rule of a standard chess game.
        /// </summary>
        private readonly CheckRule checkRule;

        /// <summary>
        /// Represents the end rule of a standard chess game.
        /// </summary>
        private readonly EndRule endRule;

        /// <summary>
        /// Represents the movement rule of a standard chess game.
        /// </summary>
        private readonly MovementRule movementRule;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardRulebook"/> class.
        /// </summary>
        public StandardRulebook()
        {
            var threatAnalyzer = new ThreatAnalyzer();
            var castlingRule = new CastlingRule(threatAnalyzer);
            var enPassantRule = new EnPassantRule();
            var promotionRule = new PromotionRule();

            this.checkRule = new CheckRule(threatAnalyzer);
            this.movementRule = new MovementRule(castlingRule, enPassantRule, promotionRule, threatAnalyzer);
            this.endRule = new EndRule(this.checkRule, this.movementRule);
        }

        /// <summary>
        /// Creates a new chess game according to the standard rulebook.
        /// </summary>
        /// <returns>The newly created chess game.</returns>
        public ChessGame CreateGame()
        {
            IEnumerable<PlacedPiece> makeBaseLine(int row, Color color)
            {
                yield return new PlacedPiece(new Position(row, 0), new Rook(color));
                yield return new PlacedPiece(new Position(row, 1), new Knight(color));
                yield return new PlacedPiece(new Position(row, 2), new Bishop(color));
                yield return new PlacedPiece(new Position(row, 3), new Queen(color));
                yield return new PlacedPiece(new Position(row, 4), new King(color));
                yield return new PlacedPiece(new Position(row, 5), new Bishop(color));
                yield return new PlacedPiece(new Position(row, 6), new Knight(color));
                yield return new PlacedPiece(new Position(row, 7), new Rook(color));
            }

            IEnumerable<PlacedPiece> makePawns(int row, Color color) =>
                Enumerable.Range(0, 8).Select(
                    i => new PlacedPiece(new Position(row, i), new Pawn(color))
                );

            IImmutableDictionary<Position, ChessPiece> makePieces(int pawnRow, int baseRow, Color color)
            {
                var pawns = makePawns(pawnRow, color);
                var baseLine = makeBaseLine(baseRow, color);
                var pieces = baseLine.Union(pawns);
                var empty = ImmutableSortedDictionary.Create<Position, ChessPiece>(PositionComparer.DefaultComparer);
                return pieces.Aggregate(empty, (s, p) => s.Add(p.Position, p.Piece));
            }
            
            var whitePlayer = new Player(Color.White);
            var whitePieces = makePieces(1, 0, Color.White);
            var blackPlayer = new Player(Color.Black);
            var blackPieces = makePieces(6, 7, Color.Black);
            var board = new Board(whitePieces.AddRange(blackPieces));

            return new ChessGame(board, whitePlayer, blackPlayer);
        }

        public ChessGame Create960Game()
		{
            List<PlacedPiece> placedPieces = new List<PlacedPiece>();

            //create a list of positions, then as pieces are added, remove that psotion from the list
            IEnumerable<PlacedPiece> make960Line(int row, Color color)
            {
                List<Position> positionList = new List<Position>();
                List<Position> bishopList = new List<Position>();

                positionList.Add(new Position(row, 0));
                positionList.Add(new Position(row, 1));
                positionList.Add(new Position(row, 2));
                positionList.Add(new Position(row, 3));
                positionList.Add(new Position(row, 4));
                positionList.Add(new Position(row, 5));
                positionList.Add(new Position(row, 6));
                positionList.Add(new Position(row, 7));

                //generate random number from list
                //remove position from list
                //repeat
                Random random = new Random();
                int index = 0;
                int previousIndex = 0;
                bool g;

                //spawn king, can't spawn in corner
                index = random.Next(1, positionList.Count - 1);
                placedPieces.Add(new PlacedPiece(positionList[index], new King(color)));
                yield return new PlacedPiece(positionList[index], new King(color));
                positionList.RemoveAt(index);

                //spawn left rook
                previousIndex = index;
                index = random.Next(0, previousIndex);
                placedPieces.Add(new PlacedPiece(positionList[index], new Rook(color)));
                yield return new PlacedPiece(positionList[index], new Rook(color));
                positionList.RemoveAt(index);


                //spawn right rook
                index = random.Next(previousIndex - 1, positionList.Count);
                placedPieces.Add(new PlacedPiece(positionList[index], new Rook(color)));
                yield return new PlacedPiece(positionList[index], new Rook(color));
                positionList.RemoveAt(index);


                //spawn bishop, know if bishop spawned on white of black
                index = random.Next(0, positionList.Count);
                if (positionList[index].Column % 2 == 0) { g = true; } else { g = false; }
                placedPieces.Add(new PlacedPiece(positionList[index], new Bishop(color)));
                yield return new PlacedPiece(positionList[index], new Bishop(color));
                positionList.RemoveAt(index);

                //other bishop
                if (!g)
                {
                    foreach (Position position in positionList)
                    {
                        if (position.Column % 2 == 0)
                        {
                            bishopList.Add(position);
                        }
                    }
                }
                if (g)
                {
                    foreach (Position position in positionList)
                    {
                        if (position.Column % 2 != 0)
                        {
                            bishopList.Add(position);
                        }
                    }
                }
                index = random.Next(0, bishopList.Count);
                placedPieces.Add(new PlacedPiece(positionList[index], new Bishop(color)));
                yield return new PlacedPiece(bishopList[index], new Bishop(color));
                positionList.Remove(bishopList[index]);

                //only three positions left
                //first knight
                index = random.Next(0, positionList.Count);
                placedPieces.Add(new PlacedPiece(positionList[index], new Knight(color)));
                yield return new PlacedPiece(positionList[index], new Knight(color));
                positionList.RemoveAt(index);

                //other knight
                index = random.Next(0, positionList.Count);
                placedPieces.Add(new PlacedPiece(positionList[index], new Knight(color)));
                yield return new PlacedPiece(positionList[index], new Knight(color));
                positionList.RemoveAt(index);

                //queen
                placedPieces.Add(new PlacedPiece(positionList[0], new Queen(color)));
                yield return new PlacedPiece(positionList[0], new Queen(color));
            }

            IEnumerable<PlacedPiece> makePawns(int row, Color color) =>
               Enumerable.Range(0, 8).Select(
                   i => new PlacedPiece(new Position(row, i), new Pawn(color))
               );

            

            IImmutableDictionary<Position, ChessPiece> makePieces(int pawnRow, int baseRow, Color color)
            {
                var pawns = makePawns(pawnRow, color);
                var baseLine = make960Line(baseRow, color);
                var pieces = baseLine.Union(pawns);
                var empty = ImmutableSortedDictionary.Create<Position, ChessPiece>(PositionComparer.DefaultComparer);
                return pieces.Aggregate(empty, (s, p) => s.Add(p.Position, p.Piece));
            }

            var whitePlayer = new Player(Color.White);
            var whitePieces = makePieces(1, 0, Color.White);
            var blackPlayer = new Player(Color.Black);
            var blackPieces = makePieces(6, 7, Color.Black);
            //var blackPieces = new Dictionary<Position, ChessPiece> { { placedPieces[0].Position, placedPieces[0].Piece } };
            
            var board = new Board(whitePieces.AddRange(blackPieces));

            return new ChessGame(board, whitePlayer, blackPlayer);
		}

        /// <summary>
        /// Gets the status of a chess game, according to the standard rulebook.
        /// </summary>
        /// <param name="game">The game state to be analyzed.</param>
        /// <returns>The current status of the game.</returns>
        public Status GetStatus(ChessGame game)
        {
            return this.endRule.GetStatus(game);
        }

        /// <summary>
        /// Gets all possible updates (i.e., future game states) for a chess piece on a specified position,
        /// according to the standard rulebook.
        /// </summary>
        /// <param name="game">The current game state.</param>
        /// <param name="position">The position to be analyzed.</param>
        /// <returns>A sequence of all possible updates for a chess piece on the specified position.</returns>
        public IEnumerable<Update> GetUpdates(ChessGame game, Position position)
        {
            var piece = game.Board.GetPiece(position, game.ActivePlayer.Color);
            var updates = piece.Map(
                p =>
                {
                    var moves = this.movementRule.GetCommands(game, p);
                    var turnEnds = moves.Select(c => new SequenceCommand(c, EndTurnCommand.Instance));
                    var records = turnEnds.Select
                    (
                        c => new SequenceCommand(c, new SetLastUpdateCommand(new Update(game, c)))
                    );
                    var futures = records.Select(c => c.Execute(game).Map(g => new Update(g, c)));
                    return futures.FilterMaybes().Where
                    (
                        e => !this.checkRule.Check(e.Game, e.Game.PassivePlayer)
                    );
                }
            );

            return updates.GetOrElse(Enumerable.Empty<Update>());
        }
    }
}