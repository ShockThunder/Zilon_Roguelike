﻿using System;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using Zilon.Bot.Players;
using Zilon.Bot.Sdk;
using Zilon.Core.Scoring;
using Zilon.Core.Tactics;
using Zilon.Emulation.Common;

namespace Zilon.BotEnvironment
{
    class AutoplayEngine : AutoplayEngineBase<IPluggableActorTaskSource>
    {
        private const string SCORE_FILE_PATH = "bot-scores";
        private const int BOT_EXCEPTION_LIMIT = 3;
        private const int ENVIRONMENT_EXCEPTION_LIMIT = 3;

        private readonly Startup _startup;
        private readonly string _scoreFilePreffix;

        private int _botExceptionCount;
        private int _envExceptionCount;

        private readonly StringBuilder _logStringBuilder;

        public string LogOutput => _logStringBuilder.ToString();

        public AutoplayEngine(Startup startup,
            BotSettings botSettings,
            string scoreFilePreffix
            ) : base(botSettings)
        {
            _startup = startup;
            _scoreFilePreffix = scoreFilePreffix;
            _logStringBuilder = new StringBuilder();
        }

        protected override void CatchActorTaskExecutionException(ActorTaskExecutionException exception)
        {
            AppendException(exception, _scoreFilePreffix);

            var monsterActorTaskSource = ServiceScope.ServiceProvider.GetRequiredService<MonsterBotActorTaskSource>();
            if (exception.ActorTaskSource != monsterActorTaskSource)
            {
                _botExceptionCount++;

                if (_botExceptionCount >= BOT_EXCEPTION_LIMIT)
                {
                    AppendFail(ServiceScope.ServiceProvider, _scoreFilePreffix);
                    throw exception;
                }
            }
            else
            {
                _envExceptionCount++;
                CheckEnvExceptions(_envExceptionCount, exception);
                Log($"[.] {exception.Message}");
            }
        }

        protected override void CatchException(Exception exception)
        {
            AppendException(exception, _scoreFilePreffix);

            _envExceptionCount++;
            CheckEnvExceptions(_envExceptionCount, exception);
            Log($"[.] {exception.Message}");
        }

        protected override void ConfigBotAux()
        {
            _startup.ConfigureAux(ServiceScope.ServiceProvider);
        }

        protected override void ProcessEnd()
        {
            var mode = BotSettings.Mode;
            var scoreManager = ServiceScope.ServiceProvider.GetRequiredService<IScoreManager>();
            WriteScores(ServiceScope.ServiceProvider, scoreManager, mode, _scoreFilePreffix);
        }

        private void WriteScores(IServiceProvider serviceFactory, IScoreManager scoreManager, string mode, string scoreFilePreffix)
        {
            var summaryText = TextSummaryHelper.CreateTextSummary(scoreManager.Scores);

            Log(summaryText);

            AppendScores(scoreManager, serviceFactory, scoreFilePreffix, mode, summaryText);
        }

        private static void AppendScores(
            IScoreManager scoreManager,
            IServiceProvider serviceFactory,
            string scoreFilePreffix,
            string mode,
            string summary)
        {
            var path = SCORE_FILE_PATH;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var botTaskSource = serviceFactory.GetRequiredService<IPluggableActorTaskSource>();
            var scoreFilePreffixFileName = GetScoreFilePreffix(scoreFilePreffix);
            var filename = Path.Combine(path, $"{botTaskSource.GetType().FullName}{scoreFilePreffixFileName}.scores");
            using (var file = new StreamWriter(filename, append: true))
            {
                var fragSum = scoreManager.Frags.Sum(x => x.Value);
                file.WriteLine($"{DateTime.UtcNow}\t{scoreManager.BaseScores}\t{scoreManager.Turns}\t{fragSum}");
            }

            DatabaseContext.AppendScores(path, scoreManager, botTaskSource.GetType().FullName, scoreFilePreffix, mode, summary);
        }

        private void AppendException(Exception exception, string scoreFilePreffix)
        {
            var path = SCORE_FILE_PATH;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var botTaskSource = ServiceScope.ServiceProvider.GetRequiredService<IPluggableActorTaskSource>();
            var scoreFilePreffixFileName = GetScoreFilePreffix(scoreFilePreffix);
            var filename = Path.Combine(path, $"{botTaskSource.GetType().FullName}{scoreFilePreffixFileName}.exceptions");
            using (var file = new StreamWriter(filename, append: true))
            {
                file.WriteLine(DateTime.UtcNow);
                file.WriteLine(exception);
                file.WriteLine();
            }
        }

        private static string GetScoreFilePreffix(string scoreFilePreffix)
        {
            var scoreFilePreffixFileName = string.Empty;
            if (!string.IsNullOrWhiteSpace(scoreFilePreffix))
            {
                scoreFilePreffixFileName = $"-{scoreFilePreffix}";
            }

            return scoreFilePreffixFileName;
        }

        private void AppendFail(IServiceProvider serviceFactory, string scoreFilePreffix)
        {
            Log("[x] Bot task source error limit reached");

            var path = SCORE_FILE_PATH;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var botTaskSource = serviceFactory.GetRequiredService<IPluggableActorTaskSource>();
            var scoreFilePreffixFileName = GetScoreFilePreffix(scoreFilePreffix);
            var filename = Path.Combine(path, $"{botTaskSource.GetType().FullName}{scoreFilePreffixFileName}.scores");
            using (var file = new StreamWriter(filename, append: true))
            {
                file.WriteLine($"-1");
            }
        }

        private static void CheckEnvExceptions(int envExceptionCount, Exception exception)
        {
            if (envExceptionCount >= ENVIRONMENT_EXCEPTION_LIMIT)
            {
                throw exception;
            }
        }

        private void Log(string message)
        {
            _logStringBuilder.AppendLine(message);
        }

        protected override void ProcessSectorExit()
        {
            Log("Exit");
        }
    }
}
