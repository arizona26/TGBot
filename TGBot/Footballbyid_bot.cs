using RestSharp;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TGBot
{
    public class Footballbyid_bot
    {
        private readonly TelegramBotClient _botClient = new TelegramBotClient(Constants.Token);
        private readonly CancellationToken _cancellationToken = new CancellationToken();
        private readonly ReceiverOptions _receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        private readonly RestClient _restClient = new RestClient(Constants.Address);

        public async Task Start()
        {
            _botClient.StartReceiving(HandlerUpdateAsync, HandlerErrorAsync, _receiverOptions, _cancellationToken);
            var botMe = await _botClient.GetMeAsync();
            Console.WriteLine(botMe.Username + "started");
        }

        private Task HandlerErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ: \n{apiRequestException.ErrorCode}" + $"{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private async Task HandlerUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(_botClient, update.Message);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
                switch (message.Text.Split(' ')[0])
                {
                    case "/start":
                        await this._botClient.SendTextMessageAsync(message.Chat.Id, "Ласкаво просимо до бота для пошуку команд за ID та назвою, а також для пошуку майбутніх матчів. Для перегляду функцій бота натисніть кнопку Меню або введіть команду /help.");
                        break;

                    case "/help":
                        await this._botClient.SendTextMessageAsync(message.Chat.Id,
                            "/teamId <ID> - Знайти команду за ID\n" +
                            "/searchnextmatch <TeamID> <Number> - Пошук наступних матчів (введіть ID команди та кількість матчів)\n" +
                            "/searchbyname <TeamName> - Пошук всіх команд за назвою (наприклад, 'Manchester United')\n" +
                            "/savefavouriteteam <TeamID> - Додати улюблену команду за ID (можна додати лише одну команду)\n" +
                            "/deletefavouriteteam <ID> - Видалити улюблену команду\n" +
                            "/favouriteteam - Показати улюблену команду у Telegram боті");
                        break;

                    case "/teamId":
                        if (message.Text.Split(' ').Length == 2 && int.TryParse(message.Text.Split(' ')[1], out int teamId))
                        {
                            await GetTeamById(message);
                        }
                        else
                        {
                            await this._botClient.SendTextMessageAsync(message.Chat.Id, "Будь ласка, вкажіть правильний ID команди.");
                        }
                        break;

                    case "/searchnextmatch":
                        await SearchNextMatch(message);
                        break;

                    case "/searchbyname":
                        await SearchByName(message);
                        break;

                case "/savefavouriteteam":
                    await SaveFavouriteTeam(message);
                    break;

                case "/deletefavouriteteam":
                        await DeleteFavouriteTeam(message);
                        break;

                    case "/favouriteteam":
                        await GetFavouriteTeam(message);
                        break;

                    default:
                        await this._botClient.SendTextMessageAsync(message.Chat.Id, "Невідома команда. Будь ласка, використайте команду /help, щоб побачити список доступних команд.");
                        break;
                }
            }

        private async Task GetTeamById(Message message)
        {
            if (message.Text.Split(' ').Length == 2 && int.TryParse(message.Text.Split(' ')[1], out int teamId))
            {
                var request = new RestRequest($"/APIController/GetTeamById?teamId={teamId}", Method.Get);
                var response = await _restClient.ExecuteAsync<string>(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    var content = response.Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                    await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                }
                else
                {
                    string errorMessage = $"Failed to retrieve team information. Status: {response.StatusCode}, Error: {response.ErrorMessage}, Content: {response.Content}";
                    Console.WriteLine(errorMessage);  // Log the error to the console
                    await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Please provide a valid team ID.");
            }
        }

        private async Task SearchNextMatch(Message message)
        {
            var parts = message.Text.Split(' ');
            if (parts.Length == 3 && int.TryParse(parts[1], out int teamId) && int.TryParse(parts[2], out int number))
            {
                var request = new RestRequest($"/APIController/GetNextMatch?teamid={teamId}&number={number}", Method.Get);
                var response = await _restClient.ExecuteAsync<string>(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    var content = response.Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                    await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                }
                else
                {
                    string errorMessage = $"Failed to retrieve match information. Status: {response.StatusCode}, Error: {response.ErrorMessage}, Content: {response.Content}";
                    Console.WriteLine(errorMessage);  // Log the error to the console
                    await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Please provide information in the format 'teamId number', e.g., '772 10'.");
            }
        }

        private async Task SearchByName(Message message)
        {
            var teamName = message.Text.Substring("/searchbyname".Length).Trim();
            var request = new RestRequest($"/APIController/GetTeamByName?name={teamName}", Method.Get);
            var response = await _restClient.ExecuteAsync<string>(request);

            if (response.IsSuccessful && response.Content != null)
            {
                var content = response.Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                await _botClient.SendTextMessageAsync(message.Chat.Id, content);
            }
            else
            {
                string errorMessage = $"Failed to retrieve team information. Status: {response.StatusCode}, Error: {response.ErrorMessage}, Content: {response.Content}";
                Console.WriteLine(errorMessage);  // Log the error to the console
                await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
            }
        }

        private async Task SaveFavouriteTeam(Message message)
        {
            if (message.Text.Split(' ').Length == 2 && int.TryParse(message.Text.Split(' ')[1], out int teamId))
            {
                var request = new RestRequest($"/DatabaseController/AddTeam?Id={teamId}", Method.Post);
                var response = await _restClient.ExecuteAsync<string>(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    var content = response.Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                    await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                }
                else
                {
                    string errorMessage = $"Failed to save favorite team. Status: {response.StatusCode}, Error: {response.ErrorMessage}, Content: {response.Content}";
                    Console.WriteLine(errorMessage);  // Log the error to the console
                    await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Please provide a valid team ID.");
            }
        }

        private async Task DeleteFavouriteTeam(Message message)
        {
            if (message.Text.Split(' ').Length == 2 && int.TryParse(message.Text.Split(' ')[1], out int teamId))
            {
                var request = new RestRequest($"/DatabaseController/DeleteTeam?teamId={teamId}", Method.Delete);
                var response = await _restClient.ExecuteAsync<string>(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    var content = response.Content.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\"", "");
                    await _botClient.SendTextMessageAsync(message.Chat.Id, content);
                }
                else
                {
                    string errorMessage = $"Failed to delete favorite team. Status: {response.StatusCode}, Error: {response.ErrorMessage}, Content: {response.Content}";
                    Console.WriteLine(errorMessage);  // Log the error to the console
                    await _botClient.SendTextMessageAsync(message.Chat.Id, errorMessage);
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Please provide a valid team ID.");
            }
        }
        private async Task GetFavouriteTeam(Message message)
        {
            var request = new RestRequest($"/DatabaseController/GetFavoriteTeam?userId={message.Chat.Id}", Method.Get);
            var response = await _restClient.ExecuteAsync<string>(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Отримуємо відповідь від сервера та видаляємо зайві роздільники
                var content = response.Content.Replace("+", "");
                await _botClient.SendTextMessageAsync(message.Chat.Id, content);
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Улюблена команда не знайдена.");
            }
        }
    }
}
