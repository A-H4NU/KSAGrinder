namespace KSAGrinder.Components
{
    public readonly struct Lecture
    {
        public Lecture(string code, Department department, string name, int grade, int numClass)
        {
            Code = code;
            Department = department;
            Name = name;
            Grade = grade;
            NumClass = numClass;
        }

        public string Code { get; }
        public Department Department { get; }
        public string Name { get; }
        public int Grade { get; }
        public int NumClass { get; }
    }
}
