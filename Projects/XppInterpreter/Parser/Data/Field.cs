namespace XppInterpreter.Parser.Data
{
    public class Field
    {
        public string Name { get; }
        public string TableVarName { get; }
        public bool IsQualified => !string.IsNullOrEmpty(TableVarName);

        public Field(string name)
        {
            Name = name;
        }

        public Field(string tableVarName, string name) : this(name)
        {
            TableVarName = tableVarName;
        }
    }
}
