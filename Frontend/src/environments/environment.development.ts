export const environment = 
{
  production: false,
  getApiUrl: () => {
    if (typeof window !== 'undefined') {
      const port = window.location.port;
      // Matches your local development ports
      if (port === '7089' || port === '5000' || port === '5016') {
        return `${window.location.origin}/api`;
      }
    }
    return 'https://localhost:7089/api';
  }
};
