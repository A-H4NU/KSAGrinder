namespace KSAGrinder.Components
{
    public readonly struct Lecture
    {
        public Lecture(string code, string department, string name, int numClass)
        {
            Code = code;
            Department = department;
            Name = name;
            NumClass = numClass;
        }

        public string Code { get; }
        public string Department { get; }
        public string Name { get; }
        public int NumClass { get; }
    }
}
