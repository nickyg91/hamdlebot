export interface ITwitchOAuthToken {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  token_type: string;
  id_token: string;
  scopes: string[];
}
