using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using TicTacToeServis.MessageBrocker;
using TicTacToeServis.Models;

namespace TicTacToeServis
{
    class TicTacToeGame
    {
        private static ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
        private static IDatabase dbRedis = redis.GetDatabase();

        private readonly RabbitService? _rabbitService;
        private enum GameResult
        {
            InProgress,
            Win,
            Draw,
            Error
        }

        public TicTacToeGame(RabbitService? rabbitService = null)
        {
            _rabbitService = rabbitService;
        }

        public async Task TicTacToe(string message)
        {
            try
            {
                var userAnswer = JsonSerializer.Deserialize<DTOConsume>(message);

                if (userAnswer == null)
                {
                    Console.WriteLine("Не удалось десериализовать: объект оказался null");
                    await _rabbitService.SendError("Tic Tac Toe error: Не удалось десериализовать: объект оказался null") ;
                }
                else
                {
                    var userId = userAnswer.UserId;
                    var partnerId = (long)dbRedis.HashGet(key: "anonimChats",
                                                          hashField: userId);

                    if (UserHasPlayingField(userId: userId) && UserHasPlayingField(userId: partnerId))
                    {
                        await HandleMove(userId, partnerId, userAnswer.Index);
                    }
                    else
                    {
                        await CreatePlayField(userId, partnerId);
                        await HandleMove(userId, partnerId, userAnswer.Index);
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка десериализации: {ex.Message}");
                await _rabbitService.SendError($"Tic Tac Toe Ошибка десериализации: {ex.Message}");
            }
        }

        private bool UserHasPlayingField(long userId)
        {
            return dbRedis.HashExists(key: "TicTacToePlayField",
                                      hashField: userId);
        }

        private async Task CreatePlayField(long userId, long partnerId)
        {
            try
            {
                string?[][] startPlayFild = new[]
                {
                    new string?[]{null, null, null},
                    new string?[]{null, null, null},
                    new string?[]{null, null, null}
                };

                var serializePlayField = JsonSerializer.Serialize(startPlayFild);

                dbRedis.HashSet("TicTacToePlayField", new HashEntry[]
                {
                    new HashEntry(userId, serializePlayField),
                    new HashEntry(partnerId, serializePlayField)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка создания игрового поля: {ex.Message}");
                await _rabbitService.SendError($"Tic Tac Toe ошибка создания игрового поля: {ex.Message}");
            }
        }

        private async Task HandleMove(long userId, long partnerId, string index)
        {
            try
            {
                var jsonUserPlayField = dbRedis.HashGet("TicTacToePlayField", userId);
                var jsonPartnerPlayField = dbRedis.HashGet("TicTacToePlayField", partnerId);
                var userRole = dbRedis.HashGet("TicTacToeRole", userId).ToString();

                if (!jsonUserPlayField.IsNullOrEmpty && !jsonPartnerPlayField.IsNullOrEmpty)
                {
                    var userPlayFieldDeserialize = JsonSerializer.Deserialize<string?[][]>(jsonUserPlayField);
                    var partnerPlayFieldDeserialize = JsonSerializer.Deserialize<string?[][]>(jsonPartnerPlayField);
                    var parts = index.Split(',');

                    int row = int.Parse(parts[0]);
                    int col = int.Parse(parts[1]);

                    if (userPlayFieldDeserialize[row][col] == null && partnerPlayFieldDeserialize[row][col] == null)
                    {
                        userPlayFieldDeserialize[row][col] = userRole;
                        partnerPlayFieldDeserialize[row][col] = userRole;

                        jsonUserPlayField = JsonSerializer.Serialize(userPlayFieldDeserialize);
                        jsonPartnerPlayField = JsonSerializer.Serialize(partnerPlayFieldDeserialize);

                        dbRedis.HashSet("TicTacToePlayField", new HashEntry[]
                        {
                            new HashEntry(userId, jsonUserPlayField),
                            new HashEntry(partnerId, jsonPartnerPlayField)
                        });
                        dbRedis.HashSet("TicTacToe", new HashEntry[]
                        {
                            new HashEntry(userId, "wait"),
                            new HashEntry(partnerId, "walk")
                        });

                        await HandleWinOrDraw(userId, partnerId, userRole);

                        Console.WriteLine($"Удачный ход по координатам: {row},{col}");
                    }
                    else
                    {
                        var message = new DTOSendToService
                        {
                            UserId = userId,
                            GameState = GameResult.Error.ToString(),
                            Message = "Ця клитінка зайнята. Оберіть іншу клітинку яка ще порожня"
                        };
                        var jsonMessage = JsonSerializer.Serialize(message); 

                        await _rabbitService.SendMessage(routingKey: "Roomfy.TicTacToe.ConsumeMessage",
                                                         message: jsonMessage);

                        Console.WriteLine("Ячейка занята");
                    }
                }
                else
                {
                    Console.WriteLine("игровое поле не найдено");
                    await _rabbitService.SendError("Tic Tac Toe игровое поле не найдено");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка хода по игровому полю: {ex.Message}");
                await _rabbitService.SendError($"ошибка хода по игровому полю: {ex.Message}");
            }
        }

        private async Task HandleWinOrDraw(long userId, long partnerId, string userSymbol)
        {
            try
            {
                var jsonUserPlayField = dbRedis.HashGet("TicTacToePlayField", userId);

                if (!jsonUserPlayField.IsNullOrEmpty)
                {
                    var userPlayField = JsonSerializer.Deserialize<string?[][]>(jsonUserPlayField);

                    // horizontal
                    if (userPlayField[0][0] == userPlayField[0][1] && userPlayField[0][1] == userPlayField[0][2])
                    {
                        if (userPlayField[0][0] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField[1][0] == userPlayField[1][1] && userPlayField[1][1] == userPlayField[1][2])
                    {
                        if (userPlayField[1][0] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField[2][0] == userPlayField[2][1] && userPlayField[2][1] == userPlayField[2][2])
                    {
                        if (userPlayField[2][0] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }//vertical
                    else if (userPlayField[0][0] == userPlayField[1][0] && userPlayField[1][0] == userPlayField[2][0])
                    {
                        if (userPlayField[0][0] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField[0][1] == userPlayField[1][1] && userPlayField[1][1] == userPlayField[2][1])
                    {
                        if (userPlayField[0][1] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField[0][2] == userPlayField[1][2] && userPlayField[1][2] == userPlayField[2][2])
                    {
                        if (userPlayField[0][2] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }//diagonal
                    else if (userPlayField[0][0] == userPlayField[1][1] && userPlayField[1][1] == userPlayField[2][2])
                    {
                        if (userPlayField[0][0] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField[0][2] == userPlayField[1][1] && userPlayField[1][1] == userPlayField[2][0])
                    {
                        if (userPlayField[0][2] != null)
                        {
                            await SendWin(userId, partnerId, userPlayField, userSymbol);
                            Console.WriteLine($"выиграл пользователь {userId} с символом {userSymbol}");
                        }
                    }
                    else if (userPlayField.All(row => row.All(cell => cell != null)))
                    {
                        await SendDrow(userId, partnerId, userPlayField, userSymbol);
                        Console.WriteLine($"ничья у пользователей {userId} & {partnerId}");
                    }
                    else
                    {
                        await SendProcesing(userId, partnerId, userPlayField, userSymbol);
                        Console.WriteLine($"игра продолжаеться для игроков {userId} & {partnerId}");
                    }
                }
                else
                {
                    Console.WriteLine("игровое поле не найдено");
                    await _rabbitService.SendError("Tic Tac Toe игровое поле не найдено");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка проверки ошибки: {ex.Message}");
                await _rabbitService.SendError($"Tic Tac Toe ошибка проверки ошибки: {ex.Message}");
            }
        }

        private async Task SendWin(long userId, long partnerId, string?[][] playField, string userSymbol)
        {
            var message = new DTOSendToService
            {
                UserId = userId,
                PartnerId = partnerId,
                ArrayPlayFild = playField,
                GameState = GameResult.Win.ToString(),
                WinerrSymbol = userSymbol
            };

            var jsonMessage = JsonSerializer.Serialize(message);

            await _rabbitService.SendMessage(routingKey: "Roomfy.TicTacToe.ConsumeMessage",
                                             message: jsonMessage);
        }

        private async Task SendDrow(long userId, long partnerId, string?[][] playField, string userSymbol)
        {
            var message = new DTOSendToService
            {
                UserId = userId,
                PartnerId = partnerId,
                ArrayPlayFild = playField,
                GameState = GameResult.Draw.ToString(),
                WinerrSymbol = userSymbol
            };

            var jsonMessage = JsonSerializer.Serialize(message);

            await _rabbitService.SendMessage(routingKey: "Roomfy.TicTacToe.ConsumeMessage",
                                             message: jsonMessage);
        }

        private async Task SendProcesing(long userId, long partnerId, string?[][] playField, string userSymbol)
        {
            var message = new DTOSendToService
            {
                UserId = userId,
                PartnerId = partnerId,
                ArrayPlayFild = playField,
                GameState = GameResult.InProgress.ToString(),
                WinerrSymbol = userSymbol
            };

            var jsonMessage = JsonSerializer.Serialize(message);

            await _rabbitService.SendMessage(routingKey: "Roomfy.TicTacToe.ConsumeMessage",
                                             message: jsonMessage);
        }
    }
}
