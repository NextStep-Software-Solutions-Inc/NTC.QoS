export type User = {
  id: string;
  authUserId: string;
  firstName?: string;
  lastName?: string;
  role: { id: string; name: string; key: string; description?: string };
  organization: { id: string; name: string; description?: string };
};

export type UserDetails = User & {};
