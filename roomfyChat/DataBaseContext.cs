using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace roomfyChat
{
    class DataBaseContext
    {
        private SqliteConnection connection;
        public bool searchResult; 

        public DataBaseContext()
        {
            string dbPath = @"roomfyDB.db";

            connection = new SqliteConnection($"Data Source={dbPath}");

            connection.Open();
        }

        public void SearchUserRegistration(string userChatId)
        {
            try
            {
                string selectQwery = "SELECT user_id FROM Users WHERE user_id = @user_id";

                using (var command = new SqliteCommand(selectQwery, connection))
                {
                    command.Parameters.AddWithValue("@user_id", userChatId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string foundUser_id = reader["user_id"].ToString() ?? "0";

                                if (userChatId == foundUser_id)
                                {
                                    searchResult = true;
                                    return;
                                }
                            }
                        }
                        else
                        {
                            searchResult = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                searchResult = false;
                Console.WriteLine($"Помилка{ex.Message}");
            }
        }

        public void AddNewUser(RegistrationData regData)
        {
            Console.WriteLine($"message from db context\t user id: {regData.userId}" +
                                $" oblast: {regData.oblast}" +
                                $" info: {regData.infoReaded}");

            try
            {
                string insertQwery = "INSERT INTO Users (user_id, oblasty, idea) VALUES (@user_id, @oblasty, @idea)";

                using (var command = new SqliteCommand(insertQwery, connection))
                {
                    command.Parameters.AddWithValue("@user_id", regData.userId);
                    command.Parameters.AddWithValue("@oblasty", regData.oblast);
                    command.Parameters.AddWithValue("@idea", regData.infoReaded);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка{ex.Message}");
            }
        }

        public int GetGameIdWithName(string gameName)
        {
            try
            {
                string selectQwery = "SELECT game_id FROM Games WHERE game_name = @game_name";
                using (var command = new SqliteCommand(selectQwery, connection))
                {
                    command.Parameters.AddWithValue("@game_name", gameName);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка{ex.Message}");
            }

            return -1;
        }


        public string ShowDiscriptionGame(string gameName)
        {
            try
            {
                string selectQwery = "SELECT discription FROM Games WHERE game_name = @game_name";

                using (var command = new SqliteCommand(selectQwery, connection))
                {
                    command.Parameters.AddWithValue("@game_name", gameName);

                    using(var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["discription"].ToString() ?? string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка{ex.Message}");
            }

            return string.Empty;
        }

        public string[] GetGameName()
        {
            List<string> titles = new List<string>();

            try
            {
                string selectQwery = "SELECT titles FROM Games";

                using (var command = new SqliteCommand(selectQwery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        titles.Add(reader["titles"].ToString() ?? "0");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка{ex.Message}");
            }

            return titles.ToArray();
        }


        public void CloseConection()
        {
            connection.Close();
        }
    }
}
