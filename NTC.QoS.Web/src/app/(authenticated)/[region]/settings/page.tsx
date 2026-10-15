import { redirect } from "next/navigation";

const defaultRoute = "settings/providers-and-services"; // Change to your actual default route

export default function SettingsPage() {
  redirect(defaultRoute);
}
