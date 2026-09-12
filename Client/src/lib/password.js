export const PASSWORD_MIN_LENGTH = 8;

/**
 * Client-side mirror of the backend PasswordPolicy. Returns the list of
 * unmet requirements (empty array = valid). Used to show a live checklist;
 * the backend always re-validates.
 */
export function passwordIssues(password) {
  const issues = [];
  const value = password ?? '';

  if (value.length < PASSWORD_MIN_LENGTH) {
    issues.push(`At least ${PASSWORD_MIN_LENGTH} characters`);
  }
  if (!/[A-Z]/.test(value)) {
    issues.push('An uppercase letter');
  }
  if (!/[a-z]/.test(value)) {
    issues.push('A lowercase letter');
  }
  if (!/[0-9]/.test(value)) {
    issues.push('A number');
  }
  if (!/[^A-Za-z0-9]/.test(value)) {
    issues.push('A symbol');
  }

  return issues;
}

export function isPasswordValid(password) {
  return passwordIssues(password).length === 0;
}
