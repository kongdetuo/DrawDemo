using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Drawing2D
{
    public abstract class DrawingDocument
    {
        public DrawingDocument()
        {
            this.IDManager = new IDManager();
            this.Change = new DocumentChange();
        }
        private Dictionary<int, DrawingElement> copyElements;
        private Dictionary<int, DrawingElement> elements;

        public event EventHandler<DocumentChangedEventArgs> Changed;

        protected virtual void OnChanged(DocumentChangedEventArgs args)
        {
            Changed?.Invoke(this, args);
        }

        public IDManager IDManager { get; private set; }

        private bool loaded = false;

        public IEnumerable<DrawingElement> GetElements()
        {
            if (!loaded)
            {
                elements = new Dictionary<int, DrawingElement>();
                copyElements = new Dictionary<int, DrawingElement>();
                this.elements = LoadElements().ToDictionary(p => p.Id);
                this.copyElements = elements.Values.ToDictionary(p => p.Id, p => p.Clone());
                loaded = true;
                Change = new DocumentChange();
            }
            return elements.Values.Where(p => p.ParentId == -1);
        }

        public DrawingElement GetElement(int id)
        {
            if (elements.TryGetValue(id, out var value))
            {
                return value;
            }
            return null;
        }

        public virtual IEnumerable<DrawingElement> LoadElements()
        {
            return Enumerable.Empty<DrawingElement>();
        }

        private Stack<DocumentChange> Undos = new Stack<DocumentChange>();
        private Stack<DocumentChange> Redos = new Stack<DocumentChange>();
        private DocumentChange Change;
        public void Add(DrawingElement element)
        {
            // 重复添加
            if (element == null || this.elements.ContainsKey(element.Id))
                return;

            elements[element.Id] = element;
            if (copyElements.ContainsKey(element.Id)) // 有可能是删除后重新添加的，算成修改
            {
                Change.Update(copyElements[element.Id], element);
            }
            else
            {
                Change.Add(element);
            }

            foreach (var item in element.ChildIds) // 尝试修改子元素
            {
                Add(elements[item]);
            }
        }

        public void Remove(DrawingElement element)
        {
            // 判断是否是重复删除
            if (element == null || !elements.ContainsKey(element.Id))
                return;

            elements.Remove(element.Id); // 删除时先删除临时数据
            if (this.copyElements.ContainsKey(element.Id))
            {
                // 如果有上一次的数据，等提交时再删掉
                Change.Remove(copyElements[element.Id]);
            }

            foreach (var item in element.ChildIds)
            {
                Remove(elements[item]);
            }
        }


        public void Update(DrawingElement newElement)
        {
            if (newElement == null)
            {
                return;
            }
            elements[newElement.Id] = newElement;
            if (copyElements.ContainsKey(newElement.Id))
            {
                var old = copyElements[newElement.Id];
                Change.Update(old, newElement);
            }
            else
            {
                Change.Add(newElement);
            }
        }

        public void SaveChanges()
        {
            var change = this.Change;
            Applay(change);
            Undos.Push(change);
            Redos.Clear();
        }

        private void Applay(DocumentChange change)
        {
            this.Change = new DocumentChange();

            foreach (var item in change.GetAddedElements().Concat(change.GetModifiedElements()))
            {
                copyElements[item.Id] = item.Clone();
            }

            foreach (var item in change.GetDeletedElements())
            {
                copyElements[item.Id] = item;
            }

            var args = new DocumentChangedEventArgs(
                change.GetAddedElements().Where(p => p.ParentId != -1).Select(p => p.Id).ToList(),
                change.GetModifiedElements().Where(p => p.ParentId != -1).Select(p => p.Id).ToList(),
                change.GetDeletedElements().Where(p => p.ParentId != -1).Select(p => p.Id).ToList());
            OnChanged(args);
        }

        public void RollBackChangs()
        {
            var change = this.Change;
            this.Change = new DocumentChange();

            foreach (var item in change.GetAddedElements())
            {
                elements.Remove(item.Id);
            }
            foreach (var item in change.GetModifiedElements())
            {
                elements[item.Id] = copyElements[item.Id].Clone();
            }
            foreach (var item in change.GetDeletedElements())
            {
                elements[item.Id] = item;
            }
        }
        public bool CanRedo() => Redos.Count > 0;


        public bool CanUndo() => Undos.Count > 0;

        public void Redo()
        {
            var changes = Redos.Pop();
            Undos.Push(changes);
            Applay(changes);
        }

        public void Undo()
        {
            var changes = Undos.Pop();
            Redos.Push(changes);
            Applay(changes.GetInverse());
        }
    }

    sealed class DocumentChange
    {
        public DocumentChange()
        {
        }
        public DocumentChange(string title)
        {
            this.Title = title;
        }

        private Dictionary<int, DrawingElement> Adds = new Dictionary<int, DrawingElement>();
        private Dictionary<int, DrawingElement> ModifyOlds = new Dictionary<int, DrawingElement>();
        private Dictionary<int, DrawingElement> ModifyNews = new Dictionary<int, DrawingElement>();
        private Dictionary<int, DrawingElement> Removes = new Dictionary<int, DrawingElement>();

        public string Title { get; set; }

        public bool CanChange { get; private set; } = true;

        public IEnumerable<DrawingElement> GetAddedElements()
        {
            return Adds.Values;
        }

        public IEnumerable<DrawingElement> GetModifiedElements()
        {
            return ModifyNews.Values;
        }

        public IEnumerable<DrawingElement> GetDeletedElements()
        {
            return Removes.Values;
        }

        public void Add(DrawingElement newElement)
        {
            if (!CanChange)
                throw new InvalidOperationException("保存后不可修改");

            Adds[newElement.Id] = newElement;
            Removes.Remove(newElement.Id); // 可能是原本准备删除的元素
        }

        public void Remove(DrawingElement oldElement)
        {
            if (!CanChange)
                throw new InvalidOperationException("保存后不可修改");

            Adds.Remove(oldElement.Id);         // 可能是原本准备添加的元素，直接删除记录即可
            ModifyOlds.Remove(oldElement.Id);   // 可能原本准备修改的元素
            ModifyNews.Remove(oldElement.Id);
            Removes[oldElement.Id] = oldElement.Clone(); // 避免被修改
        }

        public void Update(DrawingElement oldElement, DrawingElement newElement)
        {
            if (!CanChange)
                throw new InvalidOperationException("保存后不可修改");

            Removes.Remove(oldElement.Id);  // 可能是原本准备删除的元素
            ModifyOlds[oldElement.Id] = oldElement;
            ModifyNews[oldElement.Id] = newElement;
        }

        public void SaveChanges()
        {
            this.CanChange = false;
        }

        public DocumentChange GetInverse()
        {
            return new DocumentChange()
            {
                CanChange = false,
                Adds = new Dictionary<int, DrawingElement>(Removes),
                Removes = new Dictionary<int, DrawingElement>(Adds),
                ModifyNews = new Dictionary<int, DrawingElement>(ModifyOlds),
                ModifyOlds = new Dictionary<int, DrawingElement>(ModifyNews)
            };
        }
    }

    class ChangeItem
    {
        public ChangeType ChangeType { get; set; }

        public List<DrawingElement> OldElements { get; set; } = new List<DrawingElement>();
        public List<DrawingElement> NewElements { get; set; } = new List<DrawingElement>();
    }

    public class DocumentChangedEventArgs : EventArgs
    {
        public DocumentChangedEventArgs(List<int> adds, List<int> modifies, List<int> removes)
        {
            this.AddedElements = adds;
            this.ModifiedElements = modifies;
            this.RemovedElements = removes;
        }

        public IReadOnlyList<int> AddedElements { get; }
        public IReadOnlyList<int> ModifiedElements { get; }
        public IReadOnlyList<int> RemovedElements { get; }
    }

    public enum ChangeType
    {
        Add,
        Remove,
        Modify,
    }


    public class IDManager
    {
        int i = 0;
        public int NewID() => ++i;
    }


#if DEBUG

    class TestDocument : DrawingDocument
    {
        public TestDocument()
        {
        }
        public override IEnumerable<DrawingElement> LoadElements()
        {
            var list = new List<DrawingElement>();
            ////for (int i = 0; i < 20; i++)
            ////{
            ////    for (int j = 0; j < 20; j++)
            ////    {
            ////        list.Add(new TestElement(this, new Point(i * 20, j * 20)));
            ////    }
            ////}
            //AAA();
            return list;
        }

        private async void AAA()
        {
            await System.Threading.Tasks.Task.Delay(1000);

            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    new TestElement(this, new Point(i * 20, j * 20));
                    this.SaveChanges();
                    await System.Threading.Tasks.Task.Delay(100);
                }
            }
        }
    }

    class TestElement : DrawingElement
    {
        public TestElement(DrawingDocument document, System.Windows.Point point) : base(document)
        {
            this.Point = point;
            this.Pen = new Pen(Brushes.Black, 1);

            this.AddChild(new TestChildElement(document, Point));
        }

        public Point Point { get; }

        public override Geometry GetGeometry()
        {
            return new EllipseGeometry(Point, 16, 16);
        }

        public override string GetTip()
        {
            return "测试元素";
        }
    }

    class TestChildElement : DrawingElement
    {
        public TestChildElement(DrawingDocument document, System.Windows.Point point) : base(document)
        {
            this.Point = point;
            this.Pen = new Pen(Brushes.Gray, 1);

        }

        public Point Point { get; }

        public override Geometry GetGeometry()
        {
            return new EllipseGeometry(Point, 8, 8);
        }

        public override string GetTip()
        {
            return "测试元素";
        }
    }

#endif
}
