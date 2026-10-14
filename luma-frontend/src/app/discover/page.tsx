import { DiscoverFlow } from "@/components/discover/DiscoverFlow";
import { DiscoverAuthGate } from "@/components/auth/DiscoverAuthGate";

export const metadata = {
  title: "Dream Discovery — LUMA",
  description: "Begin imagining the life and rhythm you desire.",
};

export default function DiscoverPage() {
  return (
    <main className="flex-1 flex flex-col justify-center py-6 sm:py-12">
      <DiscoverAuthGate>
        <DiscoverFlow />
      </DiscoverAuthGate>
    </main>
  );
}
