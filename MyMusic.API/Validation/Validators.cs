using FluentValidation;
using MyMusic.API.Contracts;

namespace MyMusic.API.Validation;

public class SaveArtistRequestValidator : AbstractValidator<SaveArtistRequest>
{
    public SaveArtistRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(50);
    }
}

public class SaveMusicRequestValidator : AbstractValidator<SaveMusicRequest>
{
    public SaveMusicRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(50);
        RuleFor(r => r.ArtistId).NotEmpty().WithMessage("'Artist Id' must not be empty.");
    }
}

public class SaveComposerRequestValidator : AbstractValidator<SaveComposerRequest>
{
    public SaveComposerRequestValidator()
    {
        RuleFor(r => r.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(r => r.LastName).NotEmpty().MaximumLength(50);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
        RuleFor(r => r.Password).NotEmpty().MinimumLength(8);
        RuleFor(r => r.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(r => r.LastName).NotEmpty().MaximumLength(50);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty();
        RuleFor(r => r.Password).NotEmpty();
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(r => r.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(r => r.LastName).NotEmpty().MaximumLength(50);
        RuleFor(r => r.Password).MinimumLength(8).When(r => !string.IsNullOrEmpty(r.Password));
    }
}
