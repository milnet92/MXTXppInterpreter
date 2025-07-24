using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Parser
{
    public partial class XppParser
    {
        bool isParsingWhereStatement;
        bool isParsingQueryExpression;
        string currentQueryExpressionTableName;

        bool IsJoinType(Token token)
        {
            return token == Word.Join ||
                   token == Word.Outer ||
                   token == Word.Exists ||
                   token == Word.NotExists;
        }
        bool IsAggregateFunction(Token token)
        {
            return token == Word.Sum ||
                   token == Word.Avg ||
                   token == Word.Count ||
                   token == Word.MinOf ||
                   token == Word.MaxOf;
        }

        bool IsQueryModified(Token token)
        {
            return token == Word.FirstFast ||
                   token == Word.FirstOnly ||
                   token == Word.FirstOnly10 ||
                   token == Word.FirstOnly100 ||
                   token == Word.FirstOnly1000 ||
                   token == Word.ForceLiterals ||
                   token == Word.ForceNestedLoops ||
                   token == Word.ForcePlaceHolders ||
                   token == Word.ForceSelectOrder ||
                   token == Word.PessimisticLock ||
                   token == Word.OptimisticLock ||
                   token == Word.Reverse ||
                   token == Word.ForUpdate ||
                   token == Word.ValidTimeState ||
                   token == Word.CrossCompany ||
                   token == Word.RepeatableRead ||
                   token == Word.ForUpdate ||
                   token == Word.GenerateOnly;
        }

        ValidTimeState ValidTimeState()
        {
            Match(TType.ValidTimeState);
            Match(TType.LeftParenthesis);
            var fromDateExpression = Expression();
            Match(TType.Comma);
            var toDateExpression = Expression();
            Match(TType.RightParenthesis);

            return new ValidTimeState(fromDateExpression, toDateExpression);
        }

        CrossCompany CrossCompany()
        {
            Expression container = null;

            Match(TType.CrossCompany);

            if (currentToken.TokenType == TType.DoubleDot)
            {
                Match(TType.DoubleDot);
                container = Expression();
            }

            return new CrossCompany(container);
        }

        List<Field> GroupBy()
        {
            Match(TType.Group);
            Match(TType.By);

            List<Field> ret = new List<Field>();

            do
            {
                if (currentToken.TokenType == TType.Comma && ret.Count > 0)
                {
                    Match(TType.Comma);
                }

                var identifierResult = Match(TType.Id);
                var identifierName = (Word)identifierResult.Token;

                HandleMetadataInterruption(identifierResult.Line, identifierResult.Start, identifierResult.End, identifierResult.Token, Metadata.TokenMetadataType.Variable);
                Word tableNameVar = null;
                Word fieldNameVar = null;

                if (currentToken.TokenType == TType.Dot)
                {
                    tableNameVar = identifierName;

                    if (isParsingQueryExpression && tableNameVar.Lexeme !=  currentQueryExpressionTableName)
                    {
                        HandleParseError("Invalid table.");
                    }

                    Match(TType.Dot);

                    if (isParsingQueryExpression)
                    {
                        HandleAutocompletionForType(currentQueryExpressionTableName);
                    }
                    else
                    {
                        HandleAutocompletion(new Variable(tableNameVar, null, false, null));
                    }

                    fieldNameVar = (Word)Match(TType.Id).Token;
                }
                else
                {
                    fieldNameVar = identifierName;
                }

                ret.Add(new Field(tableNameVar?.Lexeme, fieldNameVar?.Lexeme));

            } while (currentToken.TokenType == TType.Comma);

            return ret;
        }

        List<OrderByField> OrderBy()
        {
            Match(TType.Order);
            Match(TType.By);

            List<OrderByField> ret = new List<OrderByField>();

            do
            {
                if (currentToken.TokenType == TType.Comma && ret.Count > 0)
                {
                    Match(TType.Comma);
                }
                var identifierResult = Match(TType.Id);
                var identifierName = (Word)identifierResult.Token;

                HandleMetadataInterruption(identifierResult.Line, identifierResult.Start, identifierResult.End, identifierResult.Token, Metadata.TokenMetadataType.Variable);
                Word tableNameVar = null;
                Word fieldNameVar = null;

                if (currentToken.TokenType == TType.Dot)
                {
                    tableNameVar = identifierName;

                    if (isParsingQueryExpression && tableNameVar.Lexeme != currentQueryExpressionTableName)
                    {
                        HandleParseError("Invalid table.");
                    }

                    Match(TType.Dot);

                    if (isParsingQueryExpression)
                    {
                        HandleAutocompletionForType(currentQueryExpressionTableName);
                    }
                    else
                    { 
                        HandleAutocompletion(new Variable(identifierName, null, false, null));
                    }

                    fieldNameVar = (Word)Match(TType.Id).Token;
                }
                else
                {
                    fieldNameVar = identifierName;
                }

                OrderByType orderByType = OrderByType.Unespecified;

                if (currentToken.TokenType == TType.Asc)
                {
                    Match(TType.Asc);
                    orderByType = OrderByType.Ascending;
                }
                else if (currentToken.TokenType == TType.Desc)
                {
                    Match(TType.Desc);
                    orderByType = OrderByType.Descending;
                }

                ret.Add(new OrderByField(tableNameVar?.Lexeme, fieldNameVar?.Lexeme, orderByType));

            } while (currentToken.TokenType == TType.Comma);

            return ret;
        }

        List<SelectionField> SelectionFields()
        {
            List<SelectionField> ret = new List<SelectionField>();
            bool selectAll = false;
            while (true)
            {
                if (IsAggregateFunction(currentToken))
                {
                    var aggregateFunctionType = currentToken.TokenType;
                    Match(currentToken.TokenType);
                    Match(TType.LeftParenthesis);
                    var fieldId = (Word)Match(TType.Id).Token;
                    Match(TType.RightParenthesis);
                    ret.Add(new SelectionField(new Field(fieldId.Lexeme), aggregateFunctionType));
                }
                else if (currentToken.TokenType == TType.Id)
                {
                    var nextToken = AdvancePeek(true).Token;

                    if (nextToken.TokenType == TType.From || nextToken.TokenType == TType.Comma)
                    {
                        ret.Add(new SelectionField(new Field((currentToken as Word).Lexeme)));
                        Match(TType.Id);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (currentToken.TokenType == TType.Star)
                {
                    Match(TType.Star);

                    if (ret.Count > 0)
                    {
                        HandleParseError("Invalid selection fields.");
                    }

                    break;
                }

                if (currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }
                else if (currentToken.TokenType != TType.From && ret.Count != 0 ||
                         (currentToken.TokenType == TType.From && ret.Count == 0 && !selectAll))
                {
                    HandleParseError("Invalid selection fields.");
                }
                else
                {
                    break;
                }
            }

            return ret;
        }

        QueryModifiersCollection QueryModifiers()
        {
            QueryModifiersCollection collection = new QueryModifiersCollection();

            while (IsQueryModified(currentToken))
            {
                if (currentToken.TokenType == TType.ValidTimeState)
                {
                    collection.ValidTimeState = ValidTimeState();
                }
                else if (currentToken.TokenType == TType.CrossCompany)
                {
                    collection.CrossCompany = CrossCompany();
                }
                else
                {
                    collection.SetFlag(currentToken.TokenType);
                    Match(currentToken.TokenType);
                }
            }

            return collection;
        }

        internal Select Select()
        {
            var start = currentScanResult;

            Match(TType.Select);
            Query query = Query();

            return new Select(query, SourceCodeBinding(start, lastScanResult));
        }

        internal SelectExpression SelectExpression()
        {
            var start = currentScanResult;

            isParsingQueryExpression = true;

            Match(TType.LeftParenthesis);
            var selectResult = Match(TType.Select);

            Query query = Query();

            isParsingQueryExpression = false;

            Match(TType.RightParenthesis);
            Match(TType.Dot);

            HandleAutocompletionForType(query.TableVariableName);

            var identifierResult = Match(TType.Id);
            var identifierName = (Word)identifierResult.Token;

            return new SelectExpression(selectResult.Token, query, identifierName.Lexeme, SourceCodeBinding(start, lastScanResult));
        }

        internal Join OuterJoin()
        {
            Match(TType.Outer);
            return Join(JoinType.Outer);
        }
        internal Join ExistsJoin()
        {
            Match(TType.Exists);
            return Join(JoinType.Exists);
        }
        internal Join NotExistsJoin()
        {
            Match(TType.NotExists);
            return Join(JoinType.NotExists);
        }

        internal Join Join(JoinType joinType)
        {
            Match(TType.Join);
            return new Join(joinType, Query());
        }

        internal Select SelectStatement()
        {
            Select select = Select();
            Match(TType.Semicolon);
            return select;
        }

        internal WhileSelect WhileSelect()
        {
            var start = Match(TType.While);
            Select select = Select();
            var end = lastScanResult;
            Block block = Block();

            return new WhileSelect(select, block, SourceCodeBinding(start, lastScanResult), SourceCodeBinding(start, end));
        }

        internal List<Setting> Settings()
        {
            List<Setting> settings = new List<Setting>();

            Match(TType.Setting);

            do
            {
                if (settings.Count != 0 && currentToken.TokenType == TType.Comma)
                {
                    Match(TType.Comma);
                }

                var fieldName = (Match(TType.Id).Token as Word).Lexeme;
                Match(TType.Assign);
                Expression expression = Expression();

                settings.Add(new Setting(fieldName, expression));

            } while (currentToken.TokenType == TType.Comma);

            return settings;
        }

        List<string> InsertFields()
        {
            List<string> ret = new List<string>();

            Match(TType.LeftParenthesis);

            do
            {
                if (ret.Count != 0)
                {
                    Match(TType.Comma);
                }

                var fieldNameToken = (Match(TType.Id).Token as Word).Lexeme;

                ret.Add(fieldNameToken);

            } while (currentScanResult.Token.TokenType != TType.RightParenthesis);

            Match(TType.RightParenthesis);

            return ret;
        }

        internal InsertRecordset InsertRecordset()
        {
            var start = currentScanResult;

            Match(TType.InsertRecordset);

            var tableVariableName = (Match(TType.Id).Token as Word).Lexeme;

            var fieldList = InsertFields();
            var select = Select();

            Match(TType.Semicolon);

            return new InsertRecordset(tableVariableName, fieldList, select, SourceCodeBinding(start, lastScanResult));
        }

        internal DeleteFrom DeleteFrom()
        {
            var start = currentScanResult;

            Match(TType.DeleteFrom);
            var tableVariableName = (Match(TType.Id).Token as Word).Lexeme;

            Where where = null;
            if (currentToken.TokenType == TType.Where)
            {
                where = Where();
            }

            Join join = null;
            if (IsJoinType(currentToken))
            {
                switch (currentToken.TokenType)
                {
                    case TType.Join: join = Join(JoinType.Regular); break;
                    case TType.Outer: join = OuterJoin(); break;
                    case TType.Exists: join = ExistsJoin(); break;
                    case TType.NotExists: join = NotExistsJoin(); break;
                }
            }

            Match(TType.Semicolon);

            return new DeleteFrom(tableVariableName, SourceCodeBinding(start, lastScanResult))
            {
                Where = where,
                Join = join
            };
        }

        internal UpdateRecordset UpdateRecordset()
        {
            var start = currentScanResult;

            Match(TType.UpdateRecordset);
            var tableVariableName = (Match(TType.Id).Token as Word).Lexeme;
            List<Setting> settings = Settings();

            Where where = null;
            if (currentToken.TokenType == TType.Where)
            {
                where = Where();
            }

            Join join = null;
            if (IsJoinType(currentToken))
            {
                switch (currentToken.TokenType)
                {
                    case TType.Join: join = Join(JoinType.Regular); break;
                    case TType.Outer: join = OuterJoin(); break;
                    case TType.Exists: join = ExistsJoin(); break;
                    case TType.NotExists: join = NotExistsJoin(); break;
                }
            }

            Match(TType.Semicolon);

            return new UpdateRecordset(tableVariableName, settings, SourceCodeBinding(start, lastScanResult))
            {
                Where = where,
                Join = join
            };
        }


        Where Where()
        {
            isParsingWhereStatement = true;

            Match(TType.Where);
            var ret = new Where(Expression());

            isParsingWhereStatement = false;

            return ret;
        }

        string Index(Word tableNameVar)
        {
            Match(TType.Index);
            HandleAutocompletion(new Variable(tableNameVar, null, false, null));
            return (Match(TType.Id).Token as Word).Lexeme;
        }

        Query Query()
        {
            var modifiersCollection = QueryModifiers();
            var selectionFields = SelectionFields();

            if (currentToken.TokenType == TType.From)
            {
                Match(TType.From);
            }
            else if (selectionFields.Count > 0)
            {
                HandleParseError("Invalid selection field.");
            }

            var tableResult = Match(TType.Id);
            var tableVarName = (Word)tableResult.Token;

            if (!isParsingQueryExpression)
            { 
                HandleMetadataInterruption(tableResult.Line, tableResult.Start, tableResult.End, tableResult.Token, Metadata.TokenMetadataType.Variable);
            }
            else
            {
                currentQueryExpressionTableName = tableVarName.Lexeme;
            }

            string index = null;

            if (currentToken.TokenType == TType.Index)
            {
                index = Index(tableVarName);
            }

            // Iterate two time, since group by and order by can be in any order
            List<Field> groupBy = null;
            List<OrderByField> orderBy = null;

            while (true)
            {
                if (currentToken.TokenType == TType.Group)
                {
                    if (groupBy != null)
                    {
                        HandleParseError("Multiple group by stataments.");
                    }

                    groupBy = GroupBy();
                }
                else if (currentToken.TokenType == TType.Order)
                {
                    if (orderBy != null)
                    {
                        HandleParseError("Multiple order by stataments.");
                    }

                    orderBy = OrderBy();
                }
                else
                {
                    break;
                }
            }

            Where where = null;
            if (currentToken.TokenType == TType.Where)
            {
                where = Where();
            }

            Join join = null;
            if (IsJoinType(currentToken))
            {
                if (isParsingQueryExpression)
                {
                    HandleParseError("Data lookup statements cannot contain joins");
                }

                switch (currentToken.TokenType)
                {
                    case TType.Join: join = Join(JoinType.Regular); break;
                    case TType.Outer: join = OuterJoin(); break;
                    case TType.Exists: join = ExistsJoin(); break;
                    case TType.NotExists: join = NotExistsJoin(); break;
                }
            }

            return new Query(tableVarName.Lexeme)
            {
                CrossCompany = modifiersCollection.CrossCompany,
                ValidTimeState = modifiersCollection.ValidTimeState,
                FirstFast = modifiersCollection.FirstFast,
                ForUpdate = modifiersCollection.ForUpdate,
                NoFetch = modifiersCollection.NoFetch,
                GenerateOnly = modifiersCollection.GenerateOnly,
                OptimisticLock = modifiersCollection.OptimisticLock,
                PessimisticLock = modifiersCollection.PessimisticLock,
                RepeatableRead = modifiersCollection.RepeatableRead,
                Reverse = modifiersCollection.Reverse,
                ForcePlaceHolders = modifiersCollection.ForcePlaceHolders,
                FirstOnly = modifiersCollection.FirstOnly,
                FirstOnly10 = modifiersCollection.FirstOnly10,
                FirstOnly100 = modifiersCollection.FirstOnly100,
                FirstOnly1000 = modifiersCollection.FirstOnly1000,
                ForceLiterals = modifiersCollection.ForceLiterals,
                ForceNetsedLoops = modifiersCollection.ForceNestedLoops,
                ForceSelectOrder = modifiersCollection.ForceSelectOrder,
                SelectionFields = selectionFields,
                OrderFields = orderBy,
                GroupFields = groupBy,
                Index = index,
                Where = where,
                Join = join
            };
        }

        public TableField TableField()
        {
            var start = Match(TType.Id);

            Match(TType.Dot);

            string tableName = (start.Token as Word).Lexeme;

            HandleAutocompletionForType(tableName);

            var end = Match(TType.Id).Token as Word;

            return new TableField(start.Token, tableName, end.Lexeme, SourceCodeBinding(start, lastScanResult));
        }

        public bool IsIdentifierMatchQueryExpressionTable(string identifier)
        {
            return isParsingQueryExpression && isParsingWhereStatement &&
                identifier.Equals(currentQueryExpressionTableName, System.StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
