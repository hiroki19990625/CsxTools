namespace RoslynPad.UI;

public interface IDialog
{
    Task ShowAsync();
    void Close();
}