import Link from "next/link";

export const dynamic = "force-static";
export default function NotFound() {
  return (
    <html lang="en">
      <body>
        <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
          <div className="text-center">
            <h1 className="text-6xl font-bold text-gray-800 mb-4">404</h1>
            <h2 className="text-2xl font-semibold text-gray-600 mb-4">Page Not Found</h2>
            <p className="text-gray-500 mb-8">The page you are looking for doesn&apos;t exist or has been moved.</p>
            <Link
              href="/"
              className="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded transition duration-300">
              Return Home
            </Link>
          </div>
        </div>
      </body>
    </html>
  );
}
