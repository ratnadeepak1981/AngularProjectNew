export const environment = 
{
  production: true,
  getApiUrl: () => {
    if (typeof window !== 'undefined') {
      return `${window.location.origin}/api`;
    }
    return 'https://yourproductiondomain.com';
  }
};
