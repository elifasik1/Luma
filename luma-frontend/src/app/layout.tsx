import type { Metadata } from "next";
import { Inter, DM_Serif_Display } from "next/font/google";
import { ThemeProvider } from "@/theme/ThemeProvider";
import { TalkFab } from "@/components/ui/TalkFab";
import "./globals.css";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin"],
  display: "swap",
});

const dmSerif = DM_Serif_Display({
  variable: "--font-dm-serif",
  weight: "400",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "LUMA — See the life you want. Build it.",
  description:
    "LUMA is a future-oriented AI companion that helps you turn an imagined future into realistic goals and small actions.",
  icons: {
    icon: [
      { url: "/icon-192.png", sizes: "192x192", type: "image/png" },
    ],
    apple: [
      { url: "/apple-icon.png", sizes: "180x180", type: "image/png" },
    ],
  },
  manifest: "/manifest.webmanifest",
  applicationName: "LUMA",
  appleWebApp: {
    capable: true,
    title: "LUMA",
    statusBarStyle: "black-translucent",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${inter.variable} ${dmSerif.variable} h-full antialiased`}
    >
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html: `(function(){
              try {
                if (window.location.pathname === '/') return;
                var p = localStorage.getItem('luma_theme_palette');
                if (p) document.documentElement.setAttribute('data-theme', p);
              } catch(e){}
            })()`,
          }}
        />
      </head>
      <body className="min-h-full flex flex-col bg-luma-bg text-luma-text">
        <ThemeProvider>
          {children}
          <TalkFab />
        </ThemeProvider>
      </body>
    </html>
  );
}
