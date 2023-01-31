close all, clear all, clc
addpath(genpath('data'));

%dataPath = 'data/cube2/';

laserThres = 4;
prepare = 'sim'; 100103;%'20210731_00193_01';

switch prepare
    case 'sim'
        dataPath = 'c:\temp\';
        fileName = 'sim_2022-09-22-08-45-09-74.csv'; %lay01 does not exist in DAQ
        WCS = [0.0 0.0 0.0];
        
    case 101
        dataPath = 'data/cube01/';
        fileName = 'lay05'; %lay01 does not exist in DAQ
        WCS = [351.6 740.6 -99.64];
    case 102
        dataPath = 'data/cube01/';
        fileName = ['lay' num2str(layers(ii),'%02d')];
        WCS = [351.6 740.6 -99.64];
    case 701
        dataPath = 'data/cube07\';
        fileName = 'lay02_2020-03-22-17-50-30-37.csv';
        fileName = 'lay01';
    case 800
        dataPath = 'C:\temp\cube08\';
        fileName = 'lay05_2020-03-22-19-14-45-93.csv';
    case 1100
        dataPath = 'C:\temp\cube11\';
        fileName = 'lay_01_2020-03-22-20-34-14-27.csv';
    case 100102
        dataPath = 'data/cube01/';
        fileName = 'lay02.csv';
        WCS = [351.6 740.6 -99.64]; 
    case 100103
        dataPath = 'data/cube01/';
        fileName = 'lay03.csv';
        WCS = [351.6 740.6 -99.64];        
    case 100901
        dataPath = 'data/cube09/';
        fileName = 'lay01_2020-03-22-19-43-18-98';
        WCS = [410 760.6 -99.64];
    case 100902
        dataPath = 'data/cube09/';
        fileName = 'lay02_2020-03-22-19-45-19-53';
    case 100903
        dataPath = 'data/cube09/';
        fileName = 'lay03_2020-03-22-19-47-20-78';
    case 20201022
        dataPath = 'c:/temp/';
        fileName = '20201022_pocketContour';
        WCS = [280.5 545.0 -99.64+14.85];
        WCS = WCS + [20 30 0];
        velRapid = 2000;
    case 20201113
        dataPath = 'D:\OneDrive - Vrije Universiteit Brussel\data\milling\';
        fileName = 'contour_Pocket-afterTuning-20201113_v3';
        WCS = [300.5 575 -166.7000];
        velRapid = 2500;
        idxstart = 1000;
    case 20201117
        dataPath = 'C:/temp/';
        %fileName = 'sim_2020-11-17-20-51-27-95';
%         fileName = 'sim_2020-11-17-21-42-03-06'; %circular pocket G272
%         fileName = 'sim_2020-11-17-21-55-59-75';
%         fileName = 'sim_2020-11-18-08-13-24-76';
%         idxstart = 10000;
        fileName = 'sim_2020-11-18-08-45-01-43';
        WCS = [300.5 575 -166.7000];
        velRapid = 2500;
        idxstart = 1;
        
     case 20210720
        dataPath = 'C:/temp/';
        %fileName = 'sim_2020-11-17-20-51-27-95';
%         fileName = 'sim_2020-11-17-21-42-03-06'; %circular pocket G272
%         fileName = 'sim_2020-11-17-21-55-59-75';
%         fileName = 'sim_2020-11-18-08-13-24-76';
%         idxstart = 10000;
        fileName = 'sim_2021-07-20-08-25-45-84_000172b';
        fileName = 'sim_2021-07-20-08-25-45-84_000175';
        WCS = [300.5 575 -166.7000];
        velRapid = 2500;
        idxstart = 1;
        
    case 202107310019401
        %Weave experiment ICALEO 2021 Trochoidal 4mm wallT first layer only
        dataPath = 'E:\data-cladaq\20210731\';
        fileName = '20210731_000194_1stlay';
        WCS = [300.5 575 -166.7000];
        idxstart = 22370;
        idxend = 43980;
        
    case '20210726_00188_01'
        %Weave experiment ICALEO 2021 trochoidal path 3mm wallT 
        % Does not exist
        dataPath = 'E:\data-cladaq\20210726\';
        fileName = '';
        WCS = [349.967 -1070 -155.14];
        idxstart = 13820;
        idxend = 13820+19600;
        sampleNr = 193;
        tdatFile = 'E:\data-sensortherm\20210726\H322-11910_20210726_000188';
        
    case '20210731_00193_01'
        %Weave experiment ICALEO 2021 trochoidal path 2mm wallT 
        dataPath = 'E:\data-cladaq\20210731\';
        fileName = '20210731_000193';
        WCS = [349.967 -1070 -155.14];
%        idxstart = 13820;
%        idxend = 13820+19600;
        idxstart = 12210+1608;
        idxend = idxstart + 173291;
        sampleNr = 193;
        tdatFile = 'E:\data-sensortherm\20210731\H322-11910_20210731_000193';
        idxTempS = 4557;
        
    case '20210731_00195_01'
        %Weave experiment ICALEO 2021 trochoidal path 6mm wallT
        dataPath = 'E:\data-cladaq\20210731\';
        fileName = '20210731_000195';
        WCS = [269.9672 -1025 -155.14];
        idxstart = 150;
        idxend = 37039;%first layer %177600;
        sampleNr = 195;
        
    case '20210731_00196_01'
        %Weave experiment ICALEO 2021 rectangular path 3mm wallT first layer only
        dataPath = 'E:\data-cladaq\20210731\';
        fileName = '20210731_000196';
        WCS = [314.9672 -1025 -155.14];
        idxstart = 201;
        %idxend = 4000;
        sampleNr = 196;
        
        
    case '000475_02'    %deposition Sprocket 18
        dataPath = 'Z:\cladaq\20220919\';
        fileName = '000475_2022-09-19-08-47-14-62';
        WCS = [310 -1100 -234.0];
        idxstart = 201;
        %idxend = 4000;
        sampleNr = 475;
   
    case '000475_03'    %Milling Sprocket 18
        dataPath = 'Z:\cladaq\20220919\';
        fileName = '000475_mill_2022-09-19-14-45-37-28';
        fileName = '000474_mill_2022-09-19-15-14-41-2';
        WCS = [310 -1100 -234.0];
        idxstart = 201;
        %idxend = 4000;
        sampleNr = 475;
        
        
        
end

if ~exist('velRapid','var')
    velRapid = 2500;
end
if ~exist('idxstart','var')
    idxstart = 1;
end

data_orig = readCSV([dataPath fileName]); %importfile(fileName, dataLines)
d = data_orig;
d.PosX = d.PosX - WCS(1)*ones(size(d.PosX));
d.PosY = d.PosY - WCS(2)*ones(size(d.PosY));
d.PosZ = d.PosZ - WCS(3)*ones(size(d.PosZ));



r = 1:size(d,1)-1;      %range after calculating velocity
rl = unique([find(d.LaserPcmd>laserThres)]);%find(d.LaserPfdbck>laserThres);
rln = setdiff(r,rl);

% d.LaserPcmd(rln) = NaN*ones(length(rln),1);
% d.LaserPfdbck(rln) = NaN*ones(length(rln),1);
% d.VelCmd(rln) = NaN*ones(length(rln),1);

d.DataTime = (d.DataTime - d.DataTime(1))*1e-3;
tvec = d.DataTime;

vels = 3.000e+04*(diff(d.PosX).^2 +diff(d.PosY).^2+diff(d.PosZ).^2).^(1/2);
% vels(rln) = NaN;

clear Tsvec idx
dummy = diff(d.DataTime);
% [~,idx1,~] = unique(d.DataTime);
% % idx = find(Tsvec<5*mean(Tsvec));
idx1 = 1:length(d.DataTime);

figure, plot(d.DataTime)


%%
idx2 = find(dummy(1:end-1)>0);
if ~exist('idxend', 'var')
    idxend = length(idx2);
end
idx = idx1(idx2(idxstart:idxend));
Tsvec = dummy(idx)*1000;

tvecIdx = tvec(idx) - tvec(idx(1));

figure, plot(Tsvec),title('Check data integrity'),xlabel('Count'),ylabel('ms')
%save([dataPath fileName '_path']);

return


%%
idx2 = idx(1):1:idx(end);
idx = idx2; clear idx2
posX = d.PosX(idx);
posY = d.PosY(idx);
posZ = d.PosZ(idx);


diffX = diff(posX);
diffY = diff(posY);
diffZ = diff(posZ);
vel = vels(idx);

theta = atan2(diffY,diffX);

x = posX; y=posY; z=posZ;

clear cd
cd1 = colormap('gray'); % take your pick (doc colormap)
cd1(:,[2 3]) = zeros(size(cd1,1),2);
cd = interp1(linspace(0,max(vel),length(cd1)),cd1,vel); % map color to y values
%cd(4,:) = 255; % last column is transparency

fh = figure('Name','Positions and direction in XY plane');
colormap(cd1);
scatter3(posX,posY,posZ,[],cd,'filled');
    view(0,90), axis equal, axis tight,
    axis([6 12 8 14]);
    xlabel('X (mm)'), ylabel('Y (mm)'),
    cbh = colorbar('EastOutside');
    set(cbh,'TickLabels',[0:0.2:1]*round(max(vel)/100)*100);
    %set(cbh,'fontsize',14);
    title(cbh,'Velocity (mm/min)','fontsize',14)
    set(gca,'fontsize',16);
    title(['Sample  ' num2str(sampleNr) ])
    drawnow
%hold on,
%quiver3(posX(1:end-1),posY(1:end-1),posZ(1:end-1),...
%    cos(theta).*vel(1:end-1),sin(theta).*vel(1:end-1),zeros(size(posZ(1:end-1))),2,'.')



%%
try
    disp('> start importing sensortherm data ')
    tData = importSensortherm([tdatFile '.csv'], [2, Inf]);
    disp('> done importing.')
catch exc
    rethrow(exc);
end

tDatT = tData.Recordtime;

if ~exist('idxTempE','var')
    idxTempE = length(tDatT);
end

tDatT = datenum(tData.Recordtime);
tDatS = seconds(tDatT(idxTempS:idxTempE));
tDatS = (tDatS - tDatS(1));

figure('Position',[680 569 887 409])
yyaxis left
    ahl = gca;
    plot(tvecIdx,vel,'k','linewidth',1)
    ylabel('Velocity (mm/min)','color','k'), xlabel('Time (s)');
    ax = axis;
    ax(3) = 0; ax(4) = 1000;
    axis(ax);
    ahl.YColor = 'k';
    
yyaxis right
    ahr = gca;
    plot(tDatS,tData.Ctemp(idxTempS:idxTempE),'r','linewidth',1);
    ylabel('Temperature (°C)','Color','r');
    ahr.YColor = 'r';
set(gca,'fontsize',16)
title(['Sample ' num2str(sampleNr)])
yticks([0 1000 1500 2000])


return


%%
fh = gcf;
vidPath = ['results/' num2str(prepare)];
mkdir(vidPath);
figname = [vidPath '\' num2str(prepare) '_' num2str(round(rem(now,1)*1e6))];
savefig(fh,[figname '.fig'])
saveas(fh,[figname],'epsc')
saveas(fh,[figname '.png'])
export_fig( figname , '-pdf')


%%
figure('name','Unwrapped X-Y plane angle'); plot(unwrap(theta))
return

if sum(find(d.LaserPcmd))
    %%
    figure, title('Laser power');
    hold on;
        plot(d.LaserPcmd), %d.DataTime
    %     plot(d.DataTime,d.LaserPfdbck); 
    legend('Laser command (W)', 'Laser feedback (W)', 'Path velocity (mm/min)');
    %%
    figure, title('Selected laser on data'), hold on;
        plot(tvec(r),d.LaserPcmd(r));
        plot(tvec(r),d.LaserPfdbck(r));
        plot(tvec(r),vels(r));
        legend('Laser command (W)','Laser feedback (W)', 'Path velocity (mm/min)','location','southoutside');
        xlabel('time (s)')
        ylabel('Velocity & Power')
        set(gca,'fontsize',16)
    %%
    figure,title('Laser FDBCK vs. velocity'), plot(vels(rl),d.LaserPfdbck(rl),'.');
        xlabel('Velocity (mm/min)'), ylabel('Laser power (W)');
        set(gca,'fontsize',16);
        figure, title('Laser CMD vs. velocity'), plot(vels(rl),d.LaserPcmd(rl),'.');
        xlabel('Velocity (mm/min)'), ylabel('Laser power (W)');
        set(gca,'fontsize',16);
        %figure, title('Velocity vs X Pos.'), plot(d.PosX(rl),vels(rl),'.')
    
    %%
else
    %%
    %close all;
    clc
    velsF = NaN*ones(size(vels(idx)));
    velsR = NaN*ones(size(vels(idx)));
    velsFidx = find(vels(idx)<velRapid);
    velsRidx = find(vels(idx)>velRapid);
    velsF(velsFidx) = vels(idx(velsFidx));
    velsR(velsRidx) = vels(idx(velsRidx));

    
    figure('Name','Velocity')
    plot(tvec(idx),velsF,'r','linewidth',1);
    hold on
    plot(tvec(idx),velsR,'g--','linewidth',0.5);
    title('Velocity');
    xlabel('Time (s)'); ylabel('Velocity (m/min)');

    [Xt,Yt,Zt] = cylinder(4,10);
    Zt = Zt*20; 
    Xt = [Xt; repmat(Xt(2,:),2,1)]; Yt = [Yt;repmat(Yt(2,:),2,1)];
    Zt = [Zt; Zt(2,1)*ones(size(Zt(2,:))); 40*ones(size(Zt(2,:)))];

    Ct = 0.6*ones(size(Zt,1),size(Zt,2),3);
    Ct([1 2],:,1) = 1*ones(2,size(Zt,2));
    Ct([1 2],:,2) = 200/255*ones(2,size(Zt,2));
    Ct([1 2],:,3) = 0*ones(2,size(Zt,2));
    Xto = Xt+d.PosX(idxstart); Yto = Yt+d.PosY(idxstart); Zto = Zt + d.PosZ(idxstart);
    figure, hold on
        plot3((d.PosX(velsFidx)),(d.PosY(velsFidx)),(d.PosZ(velsFidx)),'r','linewidth',1)
        plot3(d.PosX(velsRidx),d.PosY(velsRidx),d.PosZ(velsRidx),'g--','linewidth',1)
        th = mesh(Xto,Yto,Zto);
%         tp = mesh(Xto,Yto,Zto);
        th.XDataSource = 'Xto'; th.YDataSource = 'Yto'; th.ZDataSource = 'Zto';
        th.CDataSource = 'Ct';
    axis equal, axis tight, shading interp,
    view(-60,30);
    xlabel('X (mm)'), ylabel('Y (mm)'); zlabel('Z (mm)');
    as =  alphaShape(Xto(1,:).',Yto(1,:).',Zto(1,2).');
    disp('Animation running ...');
    for ii = idxstart:4:length(d.PosX)
        Xto = [Xt+d.PosX(ii)];
        Yto = [Yt+d.PosY(ii)];
        Zto = [Zt+d.PosZ(ii)];
        k = boundary(Xto([1],:).',Yto([1],:).');
        Xto = Xto(:,k); Yto = Yto(:,k); Zto = Zto(:,k);
%         Xto = Xt+d.PosX(ii); Yto = Yt+d.PosY(ii); Zto = Zt + d.PosZ(ii);
        refreshdata(th);
        drawnow();
        pause(0.01);
    end
    disp('Done.');
end

%alphaShape



return
%%
cN = length(data2.DataTime);
imin = 518+205;
imax = N;

range = linspace(imin,imax,(imax-imin+1));

figure, plot(round((data2.DataTime(range)-min(data2.DataTime(range)))/1000))