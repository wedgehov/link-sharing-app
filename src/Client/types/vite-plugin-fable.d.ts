declare module 'vite-plugin-fable' {
  import type { Plugin } from 'vite';

  export type FableOptions = {
    fsproj?: string;
    failOnFirstError?: boolean;
    // Allow unspecified options without type errors
    [key: string]: unknown;
  };

  export default function fable(options?: FableOptions): Plugin;
}


