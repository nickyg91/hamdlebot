/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly VITE_BASE_OBS_LAYOUT_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
