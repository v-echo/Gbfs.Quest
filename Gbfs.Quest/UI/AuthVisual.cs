using Gbfs.Quest.Auth;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace Gbfs.Quest.UI
{
    internal class AuthVisual(IAuthProvider users) : IVisualTree
    {
        State<string?> user = new("");
        State<string?> password = new("");
        State<string?> error = new("");
        Dialog? dialog = null;
        bool enableValidation = false;

        public Visual GetVisual()
        {
            if (SharedState.Authenticated.Value)
            {
                return null;
            }

            ShowLoginDialog();
            return new Backdrop().IsEnabled(false);
        }

        private VStack CreateAuthVisual()
        {
            var userbox = new TextBox(user)
                .Placeholder("Enter user...")
                .Validate(user.Bind.Value, ValidateLogin);

            var passwordbox = new TextBox(password)
                .IsPassword(true)
                .ClipboardMode(TextBoxClipboardMode.Disabled)
                .Placeholder("Enter password...")
                .Validate(password.Bind.Value, ValidateLogin);

            var errorbox = new TextBox(error)
                .IsTabStop(false)
                .IsEnabled(false)
                .IsVisible(() => !string.IsNullOrWhiteSpace(error.Value) && !CanLogin());

            var login = new Button("Login").IsVisible(CanLogin).Click(TryLogin);
            var register = new Button("Register").IsVisible(CanLogin).Click(TryRegister);
            var grid = new Grid()
                .Columns(
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star(1) })
                .Rows(
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto })
                .ColumnGap(1)
                .Cell("Name:", 0, 0)
                .Cell(userbox, 0, 1)
                .Cell("Password:", 1, 0)
                .Cell(passwordbox, 1, 1)
                .HorizontalAlignment(Align.Stretch);

            var stack = new VStack(grid, errorbox, login, register).Spacing(1);

            return stack;

            bool CanLogin() => !string.IsNullOrWhiteSpace(user.Value) && !string.IsNullOrWhiteSpace(password.Value);
            ValidationMessage? ValidateLogin(string? text) => enableValidation && string.IsNullOrWhiteSpace(text) ? new ValidationMessage(ValidationSeverity.Error, new Markup("[error]Field is required.[/]") { Wrap = false }) : null;
        }

        private void ShowLoginDialog()
        {
            if (dialog is { App: not null })
            {
                return;
            }

            dialog = new()
            {
                IsModal = true,
                Width = 56,
                Height = 8,
                Title = new TextBlock("Login"),
                Content = CreateAuthVisual()
            };
            dialog.Show();
        }

        private void SetBackdropVisible(bool visible)
        {
            if (!visible)
            {
                if (dialog is not null)
                {
                    var dialog = this.dialog;
                    this.dialog = null;
                    dialog.Close();
                }
                return;
            }

            ShowLoginDialog();
        }

        private void TryLogin()
        {
            enableValidation = true;
            var result = users.Login(user.Value!, password.Value!);
            if (result)
            {
                SharedState.Authenticated.Value = true;
                SharedState.CurrentUser.Value = user.Value!;
                user.Value = string.Empty;
                password.Value = string.Empty;
                SetBackdropVisible(false);
            }
            else
            {
                error.Value = "User or password incorrect.";
                user.Value = "";
                password.Value = "";
            }
        }

        private void TryRegister()
        {
            enableValidation = true;
            var result = users.Register(user.Value!, password.Value!);
            if (result)
                TryLogin();
            else
            {
                error.Value = $"Username {user.Value!} is already in use.";
                user.Value = "";
                password.Value = "";
            }
        }
    }
}
