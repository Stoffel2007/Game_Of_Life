namespace Game_Of_Life
{
    class Field
    {
        public bool[,] field;
        public Field next;
        public string name;
        public bool borderIsActive;

        public Field(bool[,] field, string name, bool borderIsActive)
        {
            this.field = field;
            this.name  = name;
            this.borderIsActive = borderIsActive;
            next = null;
        }
    }
}