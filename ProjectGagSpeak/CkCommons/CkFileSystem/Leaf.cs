namespace GagSpeak.CkCommons.FileSystem;

public partial class FileSystem<T>
{
    public sealed class Leaf : IWritePath
    {
        /// <summary> The kind of item we are storing a reference of within the file system leaf. </summary>
        /// <remarks> A readonly reference. </remarks>
        public T Value { get; }

        internal Leaf(Folder parent, string name, T value, uint identifier)
        {
            Parent = parent;
            Value  = value;
            SetName(name);
            Identifier = identifier;
        }

        public string FullName()
            => IPath.BaseFullName(this);

        public override string ToString()
            => FullName();

        /// <summary> The leafs parent. If none, it is ROOT. </summary>
        public Folder Parent        { get; internal set; }
        public string Name          { get; private set; } = string.Empty;
        public uint   Identifier    { get; }
        public byte   Depth         { get; internal set; }
        public ushort IndexInParent { get; internal set; }
        public bool   State         { get; internal set; }

        void IWritePath.SetParent(Folder parent)
            => Parent = parent;

        internal void SetName(string name, bool fix = true)
            => Name = fix ? name.FixName() : name;

        void IWritePath.SetName(string name, bool fix)
            => SetName(name, fix);

        void IWritePath.UpdateDepth()
            => Depth = unchecked((byte)(Parent.Depth + 1));

        void IWritePath.UpdateIndex(int index)
        {
            if (index < 0)
                index = Parent.Children.IndexOf(this);
            IndexInParent = (ushort)(index < 0 ? 0 : index);
        }

        void IWritePath.UpdateState(bool state)
        {
            StaticLogger.Logger.LogDebug("Why are you trying to update the leafs state?");
        }
    }
}
