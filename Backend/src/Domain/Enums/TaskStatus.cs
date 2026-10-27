namespace FocusLab.Domain.Enums;

// Увага: в System.Threading.Tasks теж є TaskStatus. У файлах, де підключено обидва
// простори імен, використовуйте alias:
//     using TaskStatus = FocusLab.Domain.Enums.TaskStatus;
public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3
}
