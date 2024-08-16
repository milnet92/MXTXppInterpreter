namespace XppInterpreter.Interpreter.Proxy
{
    public interface IIntrinsicFunctionProvider
    {
        [IgnoreIntrinsic]
        System.Type GetCustomPredefinedFunctionProvider();

        int classNum(string name);
        string classStr(string name);
        string formStr(string name);
        string extendedTypeStr(string name);
        string menuItemActionStr(string name);
        string menuItemDisplayStr(string name);
        string menuItemOutputStr(string name);
        string methodStr(string className, string methodName);
        string staticMethodStr(string className, string methodName);
        int tableNum(string name);
        string tableStr(string name);
        object conNull();
        object dateNull();
        object maxDate();
        int maxInt();
        int minInt();
        int enumNum(string enumName);
        int configurationKeyNum(string keyName);
        string configurationKeyStr(string keyName);
        string dataEntityDataSourceStr(string dataEntity, string dataSource);
        string delegateStr(string className, string instanceDelegate);
        string dimensionHierarchyLevelStr(string dimensionHierarchyLevel);
        string dimensionHierarchyStr(string dimensionHierarchy);
        string dimensionReferenceStr(string dimensionReference);
        string dutyStr(string securityDuty);
        int enumCnt(string enumType);
        string enumLiteralStr(string enumName, string literal);
        string enumStr(string enumName);
        int extendedTypeNum(string enumName);
        int fieldNum(string tableName, string fieldName);
        string fieldPName(string tableName, string fieldName);
        string fieldStr(string talbeName, string fieldName);
        string formControlStr(string formName, string controlName);
        string formDataFieldStr(string formName, string dataSourceName, string dataFieldName);
        string formDataSourceStr(string formName, string dataSourceName);
        string formMethodStr(string formName, string methodName);
        string identifierStr(string identifierName);
        int indexNum(string talbeName, string indexName);
        string indexStr(string talbeName, string indexName);
        int licenseCodeNum(string licenseCodeName);
        string licenseCodeStr(string licenseCodeName);
        string literalStr(string literalStr);
        string measurementStr(string measurementName);
        string measureStr(string measureName);
        string menuStr(string menuName);
        string privilegeStr(string privilegeName);
        string queryDatasourceStr(string queyrName, string dataSourceName);
        string queryMethodStr(string queryName, string methodName);
        string queryStr(string queryName);
        string reportStr(string reportName);
        string resourceStr(string resourceName);
        string roleStr(string roleName);
        string ssrsReportStr(string reportName, string designName);
        string staticDelegateStr(string className, string delegateName);
        string tableCollectionStr(string tableCollectionName);
        string tableFieldGroupStr(string talbeName, string fieldGroupName);
        string tableMethodStr(string tableName, string methodName);
        string tablePName(string tableName);
        string tableStaticMethodStr(string tableName, string methodName);
        string tileStr(string tileName);
        string varStr(string varName);
        string webActionItemStr(string webActionItemName);
        string webDisplayContentItemStr(string webDisplayContentItemName);
        string webOutputContentItemStr(string webOutputContentItemName);
        string webFormStr(string webFormName);
        string webletItemStr(string webletItemName);
        string webMenuStr(string webMenuName);
        string webpageDefStr(string webpageDefName);
        string webReportStr(string webReportName);
        string websiteDefStr(string websiteDefName);
        string webSiteTempStr(string webSiteTempName);
        string webStaticFileStr(string webStaticFileName);
        string webUrlItemStr(string webUrlItemName);
        string webWebPartStr(string webWebPartName);
        string workflowapprovalstr(string workflowapprovalName);
        string workflowCategoryStr(string workflowCategoryName);
        string workflowTaskStr(string workflowTaskName);
        string workflowTypeStr(string workflowTypeName);
    }
}
