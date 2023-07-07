namespace KSAGrinder.Components
{
    public readonly record struct Lecture(
        string Code,
        Department Department,
        string Name,
        int Grade,
        int NumClass,
        int Credit);
}
