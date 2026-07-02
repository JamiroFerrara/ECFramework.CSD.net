using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ECFramework
{
    public static class DapperDbContextExtensions
    {
        // ── QueryInterpolatedStringHandler overloads ─────────────────────────────
        // The compiler auto-routes $"..." literals through QueryInterpolatedStringHandler
        // because this type carries [InterpolatedStringHandler] and appears as the
        // first parameter after 'this DbContext'. No named prefix (e.g. query$"...") needed.

        public static async Task<IEnumerable<dynamic>> QueryAsync(
            this DbContext context,
            QueryInterpolatedStringHandler handler,
            CancellationToken ct = default,
            IDbTransaction? transaction = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(context, handler, timeout, type, ct, transaction);

            var connection = context.Database.GetDbConnection();
            return await connection.QueryAsync(command.Definition);
        }

        public static IEnumerable<dynamic> Query(
            this DbContext context,
            QueryInterpolatedStringHandler handler,
            IDbTransaction? transaction = null
        )
        {
            using var command = new DapperEFCoreCommand(context, handler, timeout: null, type: null, ct: default, transaction);

            var connection = context.Database.GetDbConnection();
            return SqlMapper.Query(connection, command.Definition.CommandText, command.Definition.Parameters);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this DbContext context,
            QueryInterpolatedStringHandler handler,
            CancellationToken ct = default,
            IDbTransaction? transaction = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(context, handler, timeout, type, ct, transaction);

            var connection = context.Database.GetDbConnection();
            return await connection.QueryAsync<T>(command.Definition);
        }

        public static async Task<int> ExecuteAsync(
            this DbContext context,
            QueryInterpolatedStringHandler handler,
            CancellationToken ct = default,
            IDbTransaction? transaction = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(context, handler, timeout, type, ct, transaction);

            var connection = context.Database.GetDbConnection();
            return await connection.ExecuteAsync(command.Definition);
        }

        // ── String overloads (existing) ─────────────────────────────────────────

        public static async Task<IEnumerable<dynamic>> QueryAsync(
            this DbContext context,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(
                context, text, parameters, timeout, type, default(CancellationToken));

            var connection = context.Database.GetDbConnection();
            return await connection.QueryAsync(command.Definition);
        }

        public static IEnumerable<dynamic> Query(
            this DbContext context,
            string text,
            dynamic parameters
        )
        {
            var connection = context.Database.GetDbConnection();
            return parameters == null
                ? Dapper.SqlMapper.Query(connection, text)
                : Dapper.SqlMapper.Query(connection, text, parameters, null, true, 300);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this DbContext context,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(
                context, text, parameters, timeout, type, CancellationToken.None);

            var connection = context.Database.GetDbConnection();
            return await connection.QueryAsync<T>(command.Definition);
        }

        public static async Task<int> ExecuteAsync(
            this DbContext context,
            CancellationToken ct,
            string text,
            object parameters = null,
            int? timeout = null,
            CommandType? type = null
        )
        {
            using var command = new DapperEFCoreCommand(
                context, text, parameters, timeout, type, ct);

            var connection = context.Database.GetDbConnection();
            return await connection.ExecuteAsync(command.Definition);
        }
    }

    public readonly struct DapperEFCoreCommand : IDisposable
    {
        private readonly ILogger<DapperEFCoreCommand> _logger;

        // ── QueryInterpolatedStringHandler constructor ──────────────────────────

        public DapperEFCoreCommand(
            DbContext context,
            QueryInterpolatedStringHandler handler,
            int? timeout,
            CommandType? type,
            CancellationToken ct,
            IDbTransaction? transaction
        )
        {
            try { _logger = context.GetService<ILogger<DapperEFCoreCommand>>(); }
            catch (InvalidOperationException) { _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DapperEFCoreCommand>.Instance; }

            var tx = transaction ?? context.Database.CurrentTransaction?.GetDbTransaction();
            var commandType = type ?? CommandType.Text;
            var commandTimeout = timeout ?? context.Database.GetCommandTimeout() ?? 30;

            Definition = new CommandDefinition(
                handler.CommandText,
                handler.Parameters,
                tx,
                commandTimeout,
                commandType,
                cancellationToken: ct
            );

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    @"Executing DbCommand [CommandType='{CommandType}', CommandTimeout='{CommandTimeout}']
{CommandText}
Parameters: {@Parameters}",
                    Definition.CommandType,
                    Definition.CommandTimeout,
                    Definition.CommandText,
                    handler.Parameters);
            }
        }

        // ── String constructor (existing) ──────────────────────────────────────

        public DapperEFCoreCommand(
            DbContext context,
            string text,
            object parameters,
            int? timeout,
            CommandType? type,
            CancellationToken ct
        )
        {
            try { _logger = context.GetService<ILogger<DapperEFCoreCommand>>(); }
            catch (InvalidOperationException) { _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DapperEFCoreCommand>.Instance; }

            var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            var commandType = type ?? CommandType.Text;
            var commandTimeout = timeout ?? context.Database.GetCommandTimeout() ?? 30;

            Definition = new CommandDefinition(
                text,
                parameters,
                transaction,
                commandTimeout,
                commandType,
                cancellationToken: ct
            );

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    @"Executing DbCommand [CommandType='{CommandType}', CommandTimeout='{CommandTimeout}']
{CommandText}",
                    Definition.CommandType,
                    Definition.CommandTimeout,
                    Definition.CommandText);
            }
        }

        public CommandDefinition Definition { get; }

        public void Dispose()
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    @"Executed DbCommand [CommandType='{CommandType}', CommandTimeout='{CommandTimeout}']
{CommandText}",
                    Definition.CommandType,
                    Definition.CommandTimeout,
                    Definition.CommandText);
            }
        }
    }
}