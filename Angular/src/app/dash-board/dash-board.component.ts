import { Component, OnInit } from '@angular/core';
import { HomeOwnerService } from '../home-owner.service';
import { Face } from '../face.model';

@Component({
  selector: 'app-dash-board',
  templateUrl: './dash-board.component.html',
  styleUrls: ['./dash-board.component.css']
})
export class DashBoardComponent implements OnInit {

  constructor(private service: HomeOwnerService) { }
  NameType:any = {
    Name:"",
    Id:[]

  }
data:Face[];

  ngOnInit(): void {
    this.service.getFace().subscribe(res=>{
      this.data = res as Face[];
    },
    err=>{  
      console.log(err);
    }); 
  }

  onSubmit(){
    for(let face of this.data){
      if (face.HasName == true){
        this.NameType.Id.push(face.FaceID);
      }
    }
    this.service.postUnknown(this.NameType).subscribe(res=>{
      this.ngOnInit();
      console.log(res);
    },
    err=>{
      console.log(err);
    });
  } 
}
