import Map from "@/components/Map/Map";

export default function MapSection() {
  return (
    <section className="mx-[400px] my-5 flex h-screen" id="map">
      <div className="h-full grow">
        <Map />
      </div>
    </section>
  );
}
