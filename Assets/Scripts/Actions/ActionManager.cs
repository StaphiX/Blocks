using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction
{
    public void Run();
    public void Undo();
}

public class ActionManager
{
    Stack<IAction> undoStack = new Stack<IAction>();
    Stack<IAction> redoStack = new Stack<IAction>();

    public void AddAction(IAction action)
    {
        action.Run();
        undoStack.Push(action);
    }

    public void Undo()
    {
        IAction action = undoStack.Pop();
        action.Undo();
        redoStack.Push(action);
    }

    public void Redo()
    {
        IAction action = redoStack.Pop();
        AddAction(action);
    }
}
