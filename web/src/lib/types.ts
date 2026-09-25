// Shapes returned by the GoodMusic API (see MyMusic.API/Contracts).

export interface Artist {
  id: string;
  name: string;
  songCount?: number;
}

export interface Song {
  id: string;
  name: string;
  artist: { id: string; name: string };
}

export interface Composer {
  id: string;
  firstName: string;
  lastName: string;
}

export interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
}

export interface TokenResponse {
  token: string;
  expiresAt: string;
  user: User;
}

/** What a form action hands back to its form: field errors and/or a message. */
export interface FormState {
  message?: string;
  fieldErrors?: Record<string, string[]>;
  ok?: boolean;
}
