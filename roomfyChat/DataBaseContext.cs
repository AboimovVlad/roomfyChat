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
            string dbPath = @"D:\проекти C#\AboimovVlad\roomfyChat\roomfyChat\roomfyDB.db";

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

        public void AddNewUser(long chatId, string oblast, string game, bool infoReaded)
        {
            try
            {
                string insertQwery = "INSER INTO Users (user_id, oblasty, games, idea) VALUES (@user_id, @oblasty, @games, @idea)";
                // написать переменную найденай игры
                using (var command = new SqliteCommand(insertQwery, connection))
                {
                    command.Parameters.AddWithValue("@user_id", chatId);
                    command.Parameters.AddWithValue("@oblasty", oblast);
                    command.Parameters.AddWithValue("games", game);// переписать переменную
                    command.Parameters.AddWithValue("idea", infoReaded);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка{ex.Message}");
            }
        }

        //написать методы 1. для поиска конкретной игры 2. написать метод для вывода мисива информации о играх

        public void CloseConection()
        {
            connection.Close();
        }
    }
}
