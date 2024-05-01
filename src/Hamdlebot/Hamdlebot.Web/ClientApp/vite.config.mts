import { fileURLToPath, URL } from 'url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { join } from 'path';
import { readFileSync } from 'fs';

export default defineConfig(({ command }) => {
  if (command === 'serve') {
    const baseFolder =
      process.env.APPDATA !== undefined && process.env.APPDATA !== ''
        ? `${process.env.APPDATA}/ASP.NET/https`
        : `${process.env.HOME}/.aspnet/https`;
    const certificateName = process.env.npm_package_name;

    const certFilePath = join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = join(baseFolder, `${certificateName}.key`);
    return {
      plugins: [vue()],
      server: {
        https: {
          key: readFileSync(keyFilePath),
          cert: readFileSync(certFilePath)
        },
        port: 5002,
        strictPort: true,
        proxy: {
          '/hamdlebothub': {
            target: 'https://localhost:7256',
            changeOrigin: true,
            secure: false,
            ws: true
          },
          '/botloghub': {
            target: 'https://localhost:7256',
            changeOrigin: true,
            secure: false,
            ws: true
          },
          '/api': {
            target: 'https://localhost:7256',
            changeOrigin: true,
            secure: false
          }
        }
      },
      resolve: {
        alias: {
          '@': fileURLToPath(new URL('./src', import.meta.url))
        }
      },
      build: {
        target: 'esnext'
      }
    };
  } else {
    return {
      plugins: [vue()],
      resolve: {
        alias: {
          '@': fileURLToPath(new URL('./src', import.meta.url))
        }
      }
    };
  }
});
