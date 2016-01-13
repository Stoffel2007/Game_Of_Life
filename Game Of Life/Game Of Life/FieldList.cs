namespace Game_Of_Life
{
    class FieldList
    {
        public Field firstField;

        public FieldList()
        {
            firstField = null;
        }

        public void add(bool[,] field, string name, bool borderIsActive)
        {
            if (field != null)
            {
                Field newField = new Field(field, name, borderIsActive);
                if (firstField != null)
                {
                    Field temp = firstField;
                    while (temp.next != null)
                        temp = temp.next;
                    temp.next = newField;
                }
                else
                    firstField = newField;
            }
        }

        public Field drop()
        {
            Field lastField = null;

            if (firstField != null)
            {
                if (firstField.next != null)
                {
                    Field temp = firstField;
                    while (temp.next.next != null)
                        temp = temp.next;
                    lastField = temp.next;
                    temp.next = null;
                }
                else
                {
                    lastField = firstField;
                    firstField = null;
                }
            }

            return lastField;
        }

        public override string ToString()
        {
            string output = "";

            if (firstField == null)
                return "";
            else
            {
                Field temp = firstField;
                output = "1";
                int counter = 1;
                while (temp.next != null)
                {
                    counter++;
                    output += counter;
                    temp = temp.next;
                }
            }

            return output;
        }
    }
}