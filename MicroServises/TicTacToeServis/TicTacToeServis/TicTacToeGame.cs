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
                        // тут будет методи обработки побед и всей логики игры
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

                        await HandleWinOrDraw(userId);

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

                        await _rabbitService.SendMessage(routingKey: "Roomfy.TicTacToe.ConsumeFromSevice",
                                                         message: jsonMessage);

                        Console.WriteLine("Ячейка занята");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка хода по игровому полю: {ex.Message}");
                await _rabbitService.SendError($"ошибка хода по игровому полю: {ex.Message}");
            }
        }

        private async Task HandleWinOrDraw(long userId, string index)
        {

        }
    }
}
