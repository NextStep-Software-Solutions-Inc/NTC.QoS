type StatCardProps = { icon: React.ReactNode; label: string; value: string; range: string };

function SpeedTestStats({ icon, label, value, range }: StatCardProps) {
  return (
    <div className="flex items-start gap-3">
      <div className="bg-muted rounded-full w-8 h-8 flex items-center justify-center">{icon}</div>
      <div>
        <p className="text-sm text-muted-foreground">{label}</p>
        <p className="font-semibold">{value}</p>
        <p className="text-xs text-muted-foreground">{range}</p>
      </div>
    </div>
  );
}

export default SpeedTestStats;
