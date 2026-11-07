# Git Commit Message Guide

This guide defines the commit message format for the ASPHotelM2631 team. Clear commit messages make the project history easier to understand, review, and maintain.

## Commit message format

Use this structure:

```text
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

Example:

```text
fix(bookings): validate dates before saving a reservation

Prevent users from selecting a checkout date before the check-in date.
```

## Commit types

Use one of these types:

| Type | Use it for | Example |
|---|---|---|
| `feat` | A new user-visible feature | `feat(rooms): add room availability search` |
| `fix` | A bug correction | `fix(auth): prevent duplicate login attempts` |
| `docs` | Documentation-only changes | `docs: add commit message guide` |
| `refactor` | Code restructuring without behavior changes | `refactor(bookings): move validation into the page model` |
| `test` | Adding or changing tests | `test(rooms): add availability validation tests` |
| `build` | Project files, packages, or build configuration | `build: update vulnerable NuGet dependencies` |
| `ci` | Continuous integration or deployment changes | `ci: add pull request build workflow` |
| `style` | Formatting that does not change behavior | `style: format Razor page models` |
| `perf` | Performance improvements | `perf(search): reduce duplicate database queries` |
| `chore` | Maintenance that does not fit another type | `chore: clean generated build files` |

## Scope

The scope is optional but recommended. Use the area affected by the change, such as:

- `auth`
- `bookings`
- `rooms`
- `customers`
- `admin`
- `database`
- `ui`
- `packages`
- `docs`

Use a project-wide scope, or omit the scope, when the change affects several areas.

## Writing the subject line

- Keep it brief; aim for 50 characters and avoid exceeding 72 characters.
- Use the imperative mood: `add`, `fix`, `update`, or `remove`.
- Start with a lowercase letter after the colon.
- Do not end the subject with a period.
- Describe what the commit does, not what you personally did.

Good:

```text
feat(rooms): add room type filter
fix(bookings): prevent overlapping reservations
build(packages): update vulnerable NuGet dependencies
```

Avoid:

```text
fixed stuff
Changes
I added some code.
final final version
```

## Body

Add a body when the reason or impact is not obvious from the subject. Separate it from the subject with a blank line.

Explain:

- Why the change was needed.
- What important behavior changed.
- Any limitations or migration steps.

Example:

```text
fix(bookings): reject invalid checkout dates

Reject reservations when the checkout date is earlier than or equal to
 the check-in date. This prevents invalid records from reaching the database.
```

Do not repeat the code line by line in the body.

## Breaking changes

Clearly identify changes that require other team members to update their code, database, configuration, or workflow.

Use `!` after the type or scope:

```text
feat(database)!: rename the reservation status column
```

Explain the required action in the footer:

```text
BREAKING CHANGE: rename Reservation.Status to ReservationState.Status and run the database migration.
```

## Referencing issues or pull requests

Reference related work in the footer when applicable:

```text
Refs #42
```

Close an issue only when the commit fully resolves it:

```text
Closes #42
```

## Recommended workflow

1. Create a branch for the work, for example `feature/room-search` or `fix/booking-validation`.
2. Make a focused change. Avoid combining unrelated changes in one commit.
3. Run the application build and relevant tests.
4. Review the staged diff before committing.
5. Write a commit message that explains the change clearly.
6. Push the branch and open a pull request.

Example commands:

```powershell
git status
git diff --staged
git add <files>
git commit -m "fix(bookings): prevent overlapping reservations"
git push -u origin <branch-name>
```

## Team checklist

Before committing, confirm that:

- The message uses a valid type.
- The subject is clear, imperative, and concise.
- The commit contains one logical change.
- The code builds successfully.
- Relevant tests have been run.
- No passwords, connection strings, or other secrets are included.
- Related issues or pull requests are referenced.

## Examples for this project

```text
feat(rooms): add room availability search
fix(auth): validate login input before authentication
docs: update hotel booking setup instructions
test(bookings): cover invalid date ranges
build(packages): pin patched transitive dependencies
refactor(customers): simplify customer page model
```
