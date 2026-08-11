import { clerkMiddleware, createRouteMatcher } from "@clerk/nextjs/server";

// Define which routes are public (accessible without authentication)
const isPublicRoute = createRouteMatcher([
  "/",
  "/login(.*)",
  "/signup(.*)",
  "/public(.*)",
  "/timeout",
  "/unauthorized",
]);

// Define which routes are ignored by Clerk (e.g., Next.js internals, static assets)
// This is usually handled by the default matcher config
// const isProtectedRoute = createRouteMatcher(['/dashboard(.*)']); // Example of explicit protected route

export default clerkMiddleware(async (auth, request) => {
  // If the route is not public and the user is not authenticated,
  // redirect them to the sign-in page.
  if (!isPublicRoute(request)) {
    await auth.protect();
  }
});
export const config = {
  matcher: [
    // Match all routes except Next.js internals and files with extensions
    "/((?!.+\\.[\\w]+$|_next).*)",
    // Always run for API routes
    "/(api|trpc)(.*)",
  ],
};
