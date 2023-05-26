using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ast2
{
    public struct EditorState
    {
        public EditorState(int currentPosition, int selectionStart, int selectionEnd, List<(Type, Func<Node>)> factoryRegistry, List<Node> visibleNodesList)
        {
            CurrentPosition = currentPosition;
            SelectionStart = selectionStart;
            SelectionEnd = selectionEnd;
            FactoryRegistry = factoryRegistry;
            VisibleNodesList = visibleNodesList;
        }

        public int CurrentPosition { get; }
        
        public int SelectionStart { get; }
        
        public int SelectionEnd { get; }
        
        public List<(Type, Func<Node>)> FactoryRegistry { get; }

        public List<Node> VisibleNodesList { get; }
    }
}
