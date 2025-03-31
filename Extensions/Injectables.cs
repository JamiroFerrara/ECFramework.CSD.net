using System;
using System.Collections.Generic;

namespace ECFramework;

public static class Injectables
{
    // A list of generic actions that can apply to any entity type
    public static readonly List<Action<object, dynamic>> getItemActions = new();
    public static readonly List<Action<object, dynamic>> getPageActions = new();
    public static readonly List<Action<object, dynamic>> getExcelActions = new();
    public static readonly List<Action<object, dynamic>> deleteActions = new();
    public static readonly List<Action<object, dynamic>> createActions = new();
    public static readonly List<Action<object, dynamic>> updateActions = new();
    public static readonly List<Action<object, dynamic>> logicalDeleteActions = new();

    public static void RunGetItem(object item, dynamic context) => getItemActions.ForEach(x => x(item, context));
    public static void RunCreate(object item, dynamic context) => createActions.ForEach(x => x(item, context));
    public static void RunGetPage<E>(List<E> page, dynamic context) => getPageActions.ForEach(x => x(page, context));
    public static void RunGetExcel(object excel, dynamic context) => getExcelActions.ForEach(x => x(excel, context));
    public static void RunDelete(object item, dynamic context) => deleteActions.ForEach(x => x(item, context));
    public static void RunUpdate(object item, dynamic context) => updateActions.ForEach(x => x(item, context));
    public static void RunLogicalDelete(object item, dynamic context) => logicalDeleteActions.ForEach(x => x(item, context));

    public static void AddAction<T>(List<Action<dynamic, dynamic>> actions, Action<T, dynamic> action)
    {
        actions.Add((item, context) =>
        {
            if (item is T tAble)
                action(tAble, context);
        });
    }

    public static void AddAction(List<Action<dynamic, dynamic>> actions, Action<dynamic, dynamic> action)
    {
        actions.Add((item, context) =>
        {
            action(item, context);
        });
    }

    public static void Delete(Action<dynamic, dynamic> action) => AddAction(deleteActions, action);
    public static void GetExcel(Action<dynamic, dynamic> action) => AddAction(getExcelActions, action);
    public static void GetPage(Action<dynamic, dynamic> action) => AddAction(getPageActions, action);
    public static void GetItem(Action<dynamic, dynamic> action) => AddAction(getItemActions, action);
    public static void Create(Action<dynamic, dynamic> action) => AddAction(createActions, action);
    public static void Update(Action<dynamic, dynamic> action) => AddAction(updateActions, action);
    public static void LogicalDelete(Action<dynamic, dynamic> action) => AddAction(logicalDeleteActions, action);

    public static void Delete<T>(Action<T, dynamic> action) => AddAction(deleteActions, action);
    public static void GetExcel<T>(Action<T, dynamic> action) => AddAction(getExcelActions, action);
    public static void GetPage<T>(Action<T, dynamic> action) => AddAction(getPageActions, action);
    public static void GetItem<T>(Action<T, dynamic> action) => AddAction(getItemActions, action);
    public static void Create<T>(Action<T, dynamic> action) => AddAction(createActions, action);
    public static void Update<T>(Action<T, dynamic> action) => AddAction(updateActions, action);
    public static void LogicalDelete<T>(Action<T, dynamic> action) => AddAction(logicalDeleteActions, action);
}
