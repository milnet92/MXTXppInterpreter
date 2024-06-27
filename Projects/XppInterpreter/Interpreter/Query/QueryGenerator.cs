using System;
using System.Collections.Generic;
using XppInterpreter.Parser;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Interpreter.Query
{
    public class QueryGenerator
    {
        private readonly List<string> _tablesInQuery = new List<string>();
        private readonly RuntimeContext _context;
        private readonly QueryGenerationHelper _helper;
        public QueryGenerator(RuntimeContext runtimeContext)
        {
            _context = runtimeContext;
            _helper = new QueryGenerationHelper(runtimeContext);
        }

        Dynamics.AX.Application.SysDaFirstOnlyHint GetFirstOnlyHint(Parser.Data.Query query)
        {
            if (query.FirstOnly)
            {
                return Dynamics.AX.Application.SysDaFirstOnlyHint.FirstOnly1;
            }
            else if (query.FirstOnly10)
            {
                return Dynamics.AX.Application.SysDaFirstOnlyHint.FirstOnly10;
            }
            else if (query.FirstOnly100)
            {
                return Dynamics.AX.Application.SysDaFirstOnlyHint.FirstOnly100;
            }
            else if (query.FirstOnly1000)
            {
                return Dynamics.AX.Application.SysDaFirstOnlyHint.FirstOnly1000;
            }

            return Dynamics.AX.Application.SysDaFirstOnlyHint.None;
        }

        Dynamics.AX.Application.SysDaJoinKind GetJoinKind(Parser.Data.Join join)
        {
            switch (join.JoinType)
            {
                case Parser.Data.JoinType.Regular: return Dynamics.AX.Application.SysDaJoinKind.InnerJoin;
                case Parser.Data.JoinType.Outer: return Dynamics.AX.Application.SysDaJoinKind.OuterJoin;
                case Parser.Data.JoinType.Exists: return Dynamics.AX.Application.SysDaJoinKind.ExistsJoin;
                case Parser.Data.JoinType.NotExists: return Dynamics.AX.Application.SysDaJoinKind.NotExistsJoin;
            }

            throw new Exception("Invalid join type.");
        }

        void AddSelectionField(Dynamics.AX.Application.SysDaSelection selection, SelectionField selectionField)
        {
            switch (selectionField.AggregateFunction)
            {
                case AggregateFunction.Avg:
                    selection.addAvg(selectionField.Field.Name);
                    break;
                case AggregateFunction.Sum:
                    selection.addSum(selectionField.Field.Name);
                    break;
                case AggregateFunction.Count:
                    selection.addCount(selectionField.Field.Name);
                    break;
                case AggregateFunction.MaxOf:
                    selection.addMax(selectionField.Field.Name);
                    break;
                case AggregateFunction.MinOf:
                    selection.addMin(selectionField.Field.Name);
                    break;
                case AggregateFunction.None:
                    selection.add(selectionField.Field.Name);
                    break;
            }
        }
        public void ExecuteInsertRecordset(InsertRecordset insertRecordset)
        {
            Dynamics.AX.Application.SysDaInsertObject sysDaInsert = GenerateFromInsertRecordset(insertRecordset);
            Dynamics.AX.Application.SysDaInsertStatement insertStatement = new Dynamics.AX.Application.SysDaInsertStatement();

            insertStatement.insert(sysDaInsert);
        }

        public void ExecuteDeleteFrom(DeleteFrom deleteFrom)
        {
            Dynamics.AX.Application.SysDaDeleteObject sysDaDelete = GenerateFromDeleteRecordset(deleteFrom);
            Dynamics.AX.Application.SysDaDeleteStatement deleteStatement = new Dynamics.AX.Application.SysDaDeleteStatement();

            deleteStatement.delete(sysDaDelete);
        }

        public void ExecuteUpdateRecordset(UpdateRecordset updateRecordset)
        {
            Dynamics.AX.Application.SysDaUpdateObject sysDaUpdate = GenerateFromUpdateRecordset(updateRecordset);
            Dynamics.AX.Application.SysDaUpdateStatement updStatement = new Dynamics.AX.Application.SysDaUpdateStatement();

            updStatement.update(sysDaUpdate);
        }

        public SearchInstance NewSearchInstance(Parser.Data.Query query)
        {
            Dynamics.AX.Application.SysDaQueryObject sysDaQuery = GenerateFromQuery(query);
            Dynamics.AX.Application.SysDaSearchObject searchObject = new Dynamics.AX.Application.SysDaSearchObject(sysDaQuery);
            Dynamics.AX.Application.SysDaSearchStatement searchStatement = new Dynamics.AX.Application.SysDaSearchStatement();

            if (query.ValidTimeState != null)
            {
                searchObject.validTimeState(
                    new Dynamics.AX.Application.SysDaValidTimeStateDateRange(
                        (Microsoft.Dynamics.Ax.Xpp.AxShared.Date)_helper.ComputeVariable((Variable)query.ValidTimeState.FromDate),
                        (Microsoft.Dynamics.Ax.Xpp.AxShared.Date)_helper.ComputeVariable((Variable)query.ValidTimeState.ToDate)
                        )
                    );
            }

            if (query.CrossCompany != null)
            {
                if (query.CrossCompany.Container == null)
                {
                    searchObject.crossCompany(new Dynamics.AX.Application.SysDaCrossCompanyAll());
                }
                else
                {
                    searchObject.crossCompany(new Dynamics.AX.Application.SysDaCrossCompanyContainer(
                        (object[])_helper.ComputeVariable((Variable)query.CrossCompany.Container)));
                }
            }

            return new SearchInstance(searchObject, searchStatement);

        }

        public Dynamics.AX.Application.SysDaInsertObject GenerateFromInsertRecordset(InsertRecordset insertRecordset)
        {
            var common = (Microsoft.Dynamics.Ax.Xpp.Common)_context.ScopeHandler.CurrentScope.GetVar(insertRecordset.TableVariableName);

            _tablesInQuery.Add(insertRecordset.TableVariableName);

            Dynamics.AX.Application.SysDaInsertObject sysDaInsert = new Dynamics.AX.Application.SysDaInsertObject(common);

            // Insert fields
            foreach (var field in insertRecordset.FieldList)
            {
                // TODO: Allow variable field
                sysDaInsert.fields().add(field);
            }

            // Select query
            sysDaInsert.query(GenerateFromQuery(insertRecordset.Select.Query));

            return sysDaInsert;
        }

        public Dynamics.AX.Application.SysDaDeleteObject GenerateFromDeleteRecordset(DeleteFrom deleteFrom)
        {
            var common = (Microsoft.Dynamics.Ax.Xpp.Common)_context.ScopeHandler.CurrentScope.GetVar(deleteFrom.TableVariableName);

            _tablesInQuery.Add(deleteFrom.TableVariableName);

            Dynamics.AX.Application.SysDaQueryObject sysDaQuery = new Dynamics.AX.Application.SysDaQueryObject(common);

            // Join
            if (deleteFrom.Join != null)
            {
                sysDaQuery.joinClause(GetJoinKind(deleteFrom.Join), GenerateFromQuery(deleteFrom.Join.Select));
            }

            // Where
            if (deleteFrom.Where != null)
            {
                QueryExpressionGenerator queryExpressionGenerator = new QueryExpressionGenerator(_context, _tablesInQuery);
                sysDaQuery.whereClause(queryExpressionGenerator.Visit(deleteFrom.Where.Expression));
            }

            return new Dynamics.AX.Application.SysDaDeleteObject(sysDaQuery);
        }

        public Dynamics.AX.Application.SysDaUpdateObject GenerateFromUpdateRecordset(UpdateRecordset updateRecordset)
        {
            var common = (Microsoft.Dynamics.Ax.Xpp.Common)_context.ScopeHandler.CurrentScope.GetVar(updateRecordset.TableVariableName);

            _tablesInQuery.Add(updateRecordset.TableVariableName);

            Dynamics.AX.Application.SysDaUpdateObject sysDaUpdate = new Dynamics.AX.Application.SysDaUpdateObject(common);

            // Join
            if (updateRecordset.Join != null)
            {
                sysDaUpdate.joinClause(GetJoinKind(updateRecordset.Join), GenerateFromQuery(updateRecordset.Join.Select));
            }

            // Where
            if (updateRecordset.Where != null)
            {
                QueryExpressionGenerator queryExpressionGenerator = new QueryExpressionGenerator(_context, _tablesInQuery);
                sysDaUpdate.whereClause(queryExpressionGenerator.Visit(updateRecordset.Where.Expression));
            }

            // Settings
            foreach (var setting in updateRecordset.Settings)
            {
                QueryExpressionGenerator queryExpressionGenerator = new QueryExpressionGenerator(_context, _tablesInQuery);
                sysDaUpdate.settingClause().add(setting.FieldName, queryExpressionGenerator.Visit(setting.Expression));
            }

            return sysDaUpdate;
        }

        public Dynamics.AX.Application.SysDaQueryObject GenerateFromQuery(Parser.Data.Query query)
        {
            var common = (Microsoft.Dynamics.Ax.Xpp.Common)_context.ScopeHandler.CurrentScope.GetVar(query.TableVariableName);

            _tablesInQuery.Add(query.TableVariableName);

            Dynamics.AX.Application.SysDaQueryObject sysDaQuery = new Dynamics.AX.Application.SysDaQueryObject(common)
            {
                firstFastHint = query.FirstFast,
                firstOnlyHint = GetFirstOnlyHint(query),
                forceLiteralsHint = query.ForceLiterals,
                forceNestedLoopHint = query.ForceNetsedLoops,
                forcePlaceholdersHint = query.ForcePlaceHolders,
                forceSelectOrderHint = query.ForceSelectOrder,
                reverseHint = query.Reverse,
                forUpdateHint = query.ForUpdate,
                generateOnlyHint = query.GenerateOnly,
                optimisticLockHint = query.OptimisticLock,
                pessimisticLockHint = query.PessimisticLock,
                repeatablereadHint = query.RepeatableRead,
                noFetchHint = query.NoFetch
            };

            // Selection fields
            if (query.SelectionFields != null)
            {
                foreach (var field in query.SelectionFields)
                {
                    AddSelectionField(sysDaQuery.projection(), field);
                }
            }

            // Group by
            if (query.GroupFields != null)
            {
                foreach (var field in query.GroupFields)
                {
                    sysDaQuery.groupByClause().add(field.Name);
                }
            }

            // Order by
            if (query.OrderFields != null)
            {
                foreach (var field in query.OrderFields)
                {
                    if (field.OrderByDirection == OrderByType.Ascending || field.OrderByDirection == OrderByType.Unespecified)
                    {
                        sysDaQuery.orderByClause().add(field.Name);
                    }
                    else
                    {
                        sysDaQuery.orderByClause().addDescending(field.Name);
                    }
                }
            }

            // Join
            if (query.Join != null)
            {
                sysDaQuery.joinClause(GetJoinKind(query.Join), GenerateFromQuery(query.Join.Select));
            }

            // Where
            if (query.Where != null)
            {
                QueryExpressionGenerator queryExpressionGenerator = new QueryExpressionGenerator(_context, _tablesInQuery);
                sysDaQuery.whereClause(queryExpressionGenerator.Visit(query.Where.Expression));
            }

            return sysDaQuery;
        }
    }
}
